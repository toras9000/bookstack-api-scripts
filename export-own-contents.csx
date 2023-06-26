#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Displays a list of books accessible to API users.

var settings = new
{
    // API base address for BookStack.(Trailing slash is required.)
    ApiEntry = new Uri(@"http://localhost:9986/api/"),
};

// main processing
await Paved.RunAsync(configuration: o => o.AnyPause(), action: async () =>
{
    // Set output to UTF8 encoding.
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // Handle cancel key press
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Show caption
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Create an API client.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);

    // Determine output directory
    var outdir = ThisSource.RelativeDirectory($"{ThisSource.File().BaseName()}-{DateTime.Now:yyyyMMdd-HHmmss}");

    // Retrieve all owned book information.
    var paging = 1;
    var processed = 0;
    while (true)
    {
        // Search for own books.
        var found = await helper.Try(c => c.SearchAsync(new("{type:book} {owned_by:me}", count: 100, page: paging), signal.Token));

        // If API access is successful, scramble and save the API key.
        if (paging == 1)
        {
            await info.SaveAsync();
        }

        // Retrieve information for each book
        foreach (var item in found.books())
        {
            ConsoleWig.WriteLine($"Exporting: {item.name} ...");

            // Reading book contents
            var book = await helper.Try(c => c.ReadBookAsync(item.id, signal.Token));
            var bookDir = outdir.RelativeDirectory($"B[{book.id}].{book.name.ToFileName()}").WithCreate();
            var metaDir = bookDir.RelativeDirectory(".meta").WithCreate();
            await metaDir.RelativeFile(".book.json").WriteJsonAsync(book, signal.Token);

            // Export page content
            async Task exportPageAsync(string identify, BookContentPage pageContent)
            {
                var page = await helper.Try(c => c.ReadPageAsync(pageContent.id, signal.Token));
                await metaDir.RelativeFile($"{identify}.json").WriteJsonAsync(page, signal.Token);
                if (page.markdown.IsNotWhite()) await bookDir.RelativeFile($"{identify}_{page.name.ToFileName()}.md").WriteAllTextAsync(page.markdown, signal.Token);
                else if (page.html.IsNotWhite()) await bookDir.RelativeFile($"{identify}_{page.name.ToFileName()}.html").WriteAllTextAsync(page.html, signal.Token);

                var attachments = await helper.Try(c => c.ListAttachmentsAsync(new(filters: new Filter[] { new(nameof(AttachmentItem.uploaded_to), $"{page.id}") }), signal.Token));
                var attachfiles = attachments.data.Where(a => !a.external).ToArray();
                if (attachfiles.Length <= 0) return;

                var attachDir = bookDir.RelativeDirectory($"{identify}_attachments").WithCreate();
                foreach (var aitem in attachfiles)
                {
                    var attach = await helper.Try(c => c.ReadAttachmentAsync(aitem.id, signal.Token));
                    var bin = Convert.FromBase64String(attach.content);
                    var file = attachDir.RelativeFile($"A[{attach.id}].{attach.name.ToFileName()}".Mux(attach.extension, "."));
                    await file.WriteAllBytesAsync(bin, signal.Token);
                }
            }

            // Output the contents.
            foreach (var (content, contentIdx) in book.contents.Select((c, i) => (c, i)))
            {
                if (content is BookContentChapter chapterContent)
                {
                    var chapter = await helper.Try(c => c.ReadChapterAsync(chapterContent.id, signal.Token));
                    await metaDir.RelativeFile($"{contentIdx:D3}C.json").WriteJsonAsync(chapter, signal.Token);

                    foreach (var (pageContent, pipageIndex) in chapterContent.pages.CoalesceEmpty().Select((c, i) => (c, i)))
                    {
                        await exportPageAsync($"{contentIdx:D3}C.{pipageIndex:D3}P", pageContent);
                    }
                }
                else if (content is BookContentPage pageContent)
                {
                    await exportPageAsync($"{contentIdx:D3}P", pageContent);
                }
            }
        }

        // Update search information and determine end of search.
        paging++;
        processed += found.data.Length;
        if (found.data.Length <= 0 || found.total <= processed) break;
    }

});
