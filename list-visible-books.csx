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

/// <summary>Export Information</summary>
record SaveRecord(string Type, long ID, string Name, long Chapters, long Pages, DateTime CreateDate, DateTime UpdateDate, ExcelHyperlink URL, string? Tags)
{
    public SaveRecord(ReadBookResult book, string url)
        : this("book", book.id, book.name, book.chapters().Count(), book.pages().Count(), book.created_at, book.updated_at, new(url), formatTags(book.tags))
    { }

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

    // Show access address
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Create an API client.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);

    // Retrieve all visible book information.
    var ownlist = new List<SaveRecord>();
    var offset = 0;
    while (true)
    {
        var books = await helper.Try(c => c.ListBooksAsync(new(offset, sorts: new[] { "id", }), signal.Token));
        foreach (var book in books.data)
        {
            var detail = await helper.Try(c => c.ReadBookAsync(book.id, signal.Token));
            var record = new SaveRecord(detail, $"{settings.ApiEntry.GetLeftPart(UriPartial.Authority)}/{book.slug}");
            ownlist.Add(record);
            Console.WriteLine($"{book.id,4}: {book.name}, chapters={record.Chapters}, pages={record.Pages}");
        }

        offset += books.data.Length;
        if (books.data.Length <= 0 || books.total <= offset) break;
    }

    // If there was no book, indicate that.
    if (offset <= 0)
    {
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

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
