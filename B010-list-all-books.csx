#load ".common.csx"
#nullable enable
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Kokuban;
using Lestaly;

// Displays a list of books accessible to API users.

var settings = new
{
    // BookStack service URL.
    ServiceUrl = new Uri("http://localhost:9986/"),
};

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

    // Create client and helper
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client, signal.Token);

    // List all books
    var offset = 0;
    while (true)
    {
        // Get a list of books
        var books = await helper.Try(c => c.ListBooksAsync(new(offset, count: 500), signal.Token));
        if (books.data.Length <= 0) break;

        // Show book info
        foreach (var book in books.data)
        {
            Console.WriteLine($"Book: {Chalk.Green[book.name]}");
            var bookDetail = await helper.Try(c => c.ReadBookAsync(book.id, signal.Token));
            foreach (var content in bookDetail.contents)
            {
                if (content is BookContentChapter chapter)
                {
                    Console.WriteLine($"  Chapter: {Chalk.Blue[chapter.name]}");
                    foreach (var page in chapter.pages ?? [])
                    {
                        var draftMark = page.draft ? " (draft)" : "";
                        var templMark = page.draft ? " (template)" : "";
                        Console.WriteLine($"    Page: {Chalk.Blue[page.name]}{draftMark}{templMark}");
                    }
                }
                else if (content is BookContentPage page)
                {
                    var draftMark = page.draft ? " (draft)" : "";
                    var templMark = page.draft ? " (template)" : "";
                    Console.WriteLine($"  Page: {Chalk.Blue[page.name]}{draftMark}{templMark}");
                }
                else
                {
                    Console.WriteLine($"  Unknown: {Chalk.BrightYellow[content.name]}");
                }
            }
        }

        // Update search information and determine end of search.
        offset += books.data.Length;
        var finished = (books.data.Length <= 0) || (books.total <= offset);
        if (finished) break;
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});