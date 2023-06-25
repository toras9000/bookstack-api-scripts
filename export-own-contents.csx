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
            await bookDir.RelativeFile(".book.meta.json").WriteJsonAsync(book, signal.Token);

            // Output the contents.
            foreach (var (content, cidx) in book.contents.Select((c, i) => (c, i)))
            {
                if (content is BookContentChapter chapter)
                {
                    var chapterDetail = await helper.Try(c => c.ReadChapterAsync(chapter.id, signal.Token));
                    await bookDir.RelativeFile($".meta.{cidx:D3}C.json").WriteJsonAsync(chapterDetail, signal.Token);

                    foreach (var (page, pidx) in chapter.pages.CoalesceEmpty().Select((c, i) => (c, i)))
                    {
                        var pageDetail = await helper.Try(c => c.ReadPageAsync(page.id, signal.Token));
                        await bookDir.RelativeFile($".meta.{cidx:D3}C.{pidx:D3}P.json").WriteJsonAsync(chapterDetail, signal.Token);

                        var pageContent = (pageDetail.editor == "markdown") ? pageDetail.markdown : pageDetail.html;
                        var pageExt = (pageDetail.editor == "markdown") ? "md" : "txt";
                        await bookDir.RelativeFile($"{cidx:D3}C.{pidx:D3}P_{page.name.ToFileName()}.{pageExt}").WriteAllTextAsync(pageContent, signal.Token);
                    }
                }
                else if (content is BookContentPage page)
                {
                    var pageDetail = await helper.Try(c => c.ReadPageAsync(page.id, signal.Token));
                    await bookDir.RelativeFile($".meta.{cidx:D3}P.json").WriteJsonAsync(pageDetail, signal.Token);

                    var pageContent = (pageDetail.editor == "markdown") ? pageDetail.markdown : pageDetail.html;
                    var pageExt = (pageDetail.editor == "markdown") ? "md" : "txt";
                    await bookDir.RelativeFile($"{cidx:D3}P_{page.name.ToFileName()}.{pageExt}").WriteAllTextAsync(pageContent, signal.Token);
                }
            }
        }

        // Update search information and determine end of search.
        paging++;
        processed += found.data.Length;
        if (found.data.Length <= 0 || found.total <= processed) break;
    }

});
