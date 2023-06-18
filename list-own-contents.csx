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
    using var signal = new CancellationTokenSource();
    using var handler = ConsoleWig.CancelKeyHandlePeriod(signal);

    // Show caption
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Retrieve all books and display book names.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);

    // Retrieve all information on the books owned by the user.
    var ownbooks = new List<SearchContentBook>();
    var paging = 1;
    while (true)
    {
        var found = await helper.Try(c => c.SearchAsync(new("{type:book} {owned_by:me}", count: 100, page: paging), signal.Token));
        ownbooks.AddRange(found.books());
        paging++;
        if (found.data.Length <= 0 || found.total <= ownbooks.Count) break;
    }

    // Sort by ID
    ownbooks.Sort((b1, b2) => (int)(b1.id - b2.id));

    // Output information for each book
    foreach (var book in ownbooks)
    {
        var detail = await helper.Try(c => c.ReadBookAsync(book.id, signal.Token));
        ConsoleWig.Write($"Book.{book.id}: ").WriteLink(book.url, book.name).WriteLine("");
        foreach (var content in detail.contents)
        {
            switch (content)
            {
                case BookContentChapter chapter:
                    ConsoleWig.Write($"  Chapter.{chapter.id}: ").WriteLink(chapter.url, chapter.name).WriteLine("");
                    foreach (var page in chapter.pages.CoalesceEmpty())
                    {
                        ConsoleWig.Write($"    Page.{page.id}: ").WriteLink(page.url, page.name).WriteLine("");
                    }
                    break;
                case BookContentPage page:
                    ConsoleWig.Write($"  Page.{page.id}: ").WriteLink(page.url, page.name).WriteLine("");
                    break;
                default:
                    break;
            }
        }
    }

    // If there was no book, indicate that.
    if (ownbooks.Count <= 0)
    {
        Console.WriteLine("No books");
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
