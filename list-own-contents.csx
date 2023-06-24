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

    // Save to File
    SaveToFile = true,

    // Save to Excel file
    SaveToExcel = false,
};

/// <summary>Save Information</summary>
record SaveRecord(long? BookID, long? ChapterID, string Type, long ID, string Name, DateTime CreateDate, DateTime UpdateDate, ExcelHyperlink URL, string? Tags)
{
    public SaveRecord(ReadBookResult book, SearchContentBook additional)
        : this(book.id, null, "book", book.id, book.name, book.created_at, book.updated_at, makeLink(additional.url), formatTags(book.tags))
    { }

    public SaveRecord(BookContentChapter chapter)
        : this(chapter.book_id, chapter.id, "chapter", chapter.id, chapter.name, chapter.created_at, chapter.updated_at, makeLink(chapter.url), "(unacquired)")
    { }

    public SaveRecord(BookContentPage page)
        : this(page.book_id, page.chapter_id, "page", page.id, page.name, page.created_at, page.updated_at, makeLink(page.url), "(unacquired)")
    { }

    private static ExcelHyperlink makeLink(string url)
        => new(url);

    private static string? formatTags(Tag[]? tags)
        => tags?.Select(t => t.value.IsEmpty() ? t.name : $"{t.name}:{t.value}").JoinString(", ");
}

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

    // Retrieve all owned book information.
    var ownlist = new List<SaveRecord>();
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
            // Reading book contents
            var book = await helper.Try(c => c.ReadBookAsync(item.id, signal.Token));
            ownlist.Add(new(book, item));
            ConsoleWig.Write($"Book.{book.id}: ").WriteLink(item.url, book.name).NewLine();

            // Output the contents.
            foreach (var content in book.contents)
            {
                if (content is BookContentChapter chapter)
                {
                    ownlist.Add(new(chapter));
                    ConsoleWig.Write($"  Chapter.{chapter.id}: ").WriteLink(chapter.url, chapter.name).WriteLine("");
                    foreach (var page in chapter.pages.CoalesceEmpty())
                    {
                        ownlist.Add(new(page));
                        ConsoleWig.Write($"    Page.{page.id}: ").WriteLink(page.url, page.name).WriteLine("");
                    }
                }
                else if (content is BookContentPage page)
                {
                    ownlist.Add(new(page));
                    ConsoleWig.Write($"  Page.{page.id}: ").WriteLink(page.url, page.name).WriteLine("");
                }
            }
        }

        // Update search information and determine end of search.
        paging++;
        processed += found.data.Length;
        if (found.data.Length <= 0 || found.total <= processed) break;
    }

    if (ownlist.Count <= 0)
    {
        // If there was no book, indicate that.
        Console.WriteLine("No books");
    }
    else if (settings.SaveToFile)
    {
        // Save to file if setting is enabled
        if (settings.SaveToExcel)
        {
            var file = ThisSource.RelativeFile($"{ThisSource.File().BaseName()}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
            await ownlist.ToPseudoAsyncEnumerable().SaveToExcelAsync(file);
        }
        else
        {
            var file = ThisSource.RelativeFile($"{ThisSource.File().BaseName()}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            await ownlist.SaveToCsvAsync(file);
        }
    }

});
