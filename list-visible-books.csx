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

    // Show access address
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Retrieve all books and display book names.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);
    var offset = 0;
    while (true)
    {
        var books = await helper.Try(c => c.ListBooksAsync(new(offset, sorts: new[] { "id", }), signal.Token));
        foreach (var book in books.data)
        {
            var detail = await helper.Try(c => c.ReadBookAsync(book.id, signal.Token));
            var chapters = detail.contents.OfType<BookContentChapter>().Count();
            var pages = detail.contents.OfType<BookContentPage>().Count();
            Console.WriteLine($"{book.id,4}: {book.name}, chapters={chapters}, pages={pages}");
        }

        offset += books.data.Length;
        if (books.data.Length <= 0 || books.total <= offset) break;
    }

    // If there was no book, indicate that.
    if (offset <= 0)
    {
        Console.WriteLine("No books");
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
