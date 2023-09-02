#load ".common.csx"
#nullable enable
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Displays a list of books accessible to API users.

var settings = new
{
    // BookStack service URL.
    ServiceUrl = new Uri("http://localhost:9986/"),

    // Save to File
    SaveToFile = true,

    // Save to Excel file
    SaveToExcel = false,
};

/// <summary>Content Information</summary>
record ContentInfo(long? BookID, long? ChapterID, string Type, long ID, string Name, long? Priority, DateTime CreateDate, DateTime UpdateDate, ExcelHyperlink URL, string? Tags)
{
    public ContentInfo(ReadBookResult book, SearchContentBook additional)
        : this(book.id, null, "book", book.id, book.name, default, book.created_at, book.updated_at, makeLink(additional.url), formatTags(book.tags))
    { }

    public ContentInfo(BookContentChapter chapter)
        : this(chapter.book_id, chapter.id, "chapter", chapter.id, chapter.name, chapter.priority, chapter.created_at, chapter.updated_at, makeLink(chapter.url), "(unacquired)")
    { }

    public ContentInfo(BookContentPage page)
        : this(page.book_id, page.chapter_id, "page", page.id, page.name, page.priority, page.created_at, page.updated_at, makeLink(page.url), "(unacquired)")
    { }

    private static ExcelHyperlink makeLink(string url)
        => new(url);

    private static string? formatTags(Tag[]? tags)
        => tags?.Select(t => t.value.IsEmpty() ? t.name : $"{t.name}:{t.value}").JoinString(", ");
}

// main processing
await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Show access address
    Console.WriteLine($"Service URL : {settings.ServiceUrl}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(new(settings.ServiceUrl, "/api/"), signal.Token);

    // Create an asynchronous enumerator.
    var ownlist = enumerateOwnContents(info, signal.Token);

    // File save or display output.
    if (settings.SaveToFile)
    {
        if (settings.SaveToExcel)
        {
            var file = ThisSource.RelativeFile($"{ThisSource.File().BaseName()}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
            ConsoleWig.Write("Save to ").WriteColored(ConsoleColor.Green, file.Name).WriteLine(" ...");
            await ownlist.SaveToExcelAsync(file);
        }
        else
        {
            var file = ThisSource.RelativeFile($"{ThisSource.File().BaseName()}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            ConsoleWig.Write("Save to ").WriteColored(ConsoleColor.Green, file.Name).WriteLine(" ...");
            await ownlist.SaveToCsvAsync(file);
        }
    }
    else
    {
        ConsoleWig.WriteLine("List contents:");
        await foreach (var content in ownlist)
        {
            var indent = content.Type switch
            {
                "chapter" => "  ",
                "page" => content.ChapterID == 0 ? "  " : "    ",
                _ => "",
            };
            ConsoleWig.Write($"{indent}[{content.Type}] {content.ID}: ").WriteLink(content.URL.Target, content.Name).NewLine();
        }
    }
    ConsoleWig.WriteLine("Completed");
});

// Retrieve all owned book information.
async IAsyncEnumerable<ContentInfo> enumerateOwnContents(ApiKeyStore store, [EnumeratorCancellation] CancellationToken cancelToken)
{
    // Create an API client.
    using var client = new BookStackClient(store.ApiEntry, store.Key.Token, store.Key.Secret);
    var helper = new BookStackClientHelper(client);

    var paging = 1;
    var processed = 0;
    while (true)
    {
        // Search for own books.
        var found = await helper.Try(c => c.SearchAsync(new("{type:book} {owned_by:me}", count: 100, page: paging), cancelToken));

        // If API access is successful, scramble and save the API key.
        if (paging == 1)
        {
            await store.SaveAsync();
        }

        // Retrieve information for each book
        foreach (var item in found.books())
        {
            // Reading book contents
            var book = await helper.Try(c => c.ReadBookAsync(item.id, cancelToken));
            yield return new(book, item);

            // Output the contents.
            foreach (var content in book.contents)
            {
                if (content is BookContentChapter chapter)
                {
                    yield return new(chapter);

                    foreach (var page in chapter.pages.CoalesceEmpty())
                    {
                        yield return new(page);
                    }
                }
                else if (content is BookContentPage page)
                {
                    yield return new(page);
                }
            }
        }

        // Update search information and determine end of search.
        paging++;
        processed += found.data.Length;
        if (found.data.Length <= 0 || found.total <= processed) break;
    }
}