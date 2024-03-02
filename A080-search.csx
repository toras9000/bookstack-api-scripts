#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Sample of search

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

    // Create client
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);

    // Search
    while (true)
    {
        // input keyword
        var query = ConsoleWig.WriteLine("Search query").Write(">").ReadLine();
        if (query.IsEmpty() || query.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

        // show result
        var found = await client.SearchAsync(new(query, count: 10), signal.Token);
        if (found.data.Length <= 0)
        {
            Console.WriteLine("No results.");
        }
        else
        {
            Console.WriteLine((found.data.Length < found.total) ? $"Results (first {found.data.Length}):" : "Results:");
            foreach (var item in found.data)
            {
                Console.WriteLine($"  {item.name}: {item.url}");
            }
        }
        Console.WriteLine();
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
