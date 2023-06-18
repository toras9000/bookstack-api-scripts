#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Sample to search content

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

    // Show access address
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Create client
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);

    // search query (see https://www.bookstackapp.com/docs/user/searching/)
    {
        var query = "{type:book|chapter} B001";
        var found = await client.SearchAsync(new(query), signal.Token);
        Console.WriteLine($"Search: query={query}, result-count={found.data.Length}");
    }

    // list of chapters (see https://demo.bookstackapp.com/api/docs#listing-endpoints)
    {
        var filter = "description:like";
        var expression = "Chapter000";
        var chapters = await client.ListChaptersAsync(new(filters: new[] { new Filter(filter, expression) }), signal.Token);
        Console.WriteLine($"ListChapters: {filter}={expression}, result-count={chapters.data.Length}");
    }

    // list of pages
    {
        var filter = "name:like";
        var expression = "BP001";
        var pages = await client.ListPagesAsync(new(filters: new[] { new Filter(filter, expression) }), signal.Token);
        Console.WriteLine($"ListPages: {filter}={expression}, result-count={pages.data.Length}");
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
