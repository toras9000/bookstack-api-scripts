#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of search

var settings = new
{
    // BookStack service URL.
    ServiceUrl = new Uri("http://localhost:9986/"),
};

// main processing
return await Paved.ProceedAsync(async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Show access address
    WriteLine($"Service URL : {settings.ServiceUrl}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(new(settings.ServiceUrl, "/api/"), signal.Token);

    // Create client
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);

    // Search
    WriteLine("Search query");
    while (true)
    {
        // input keyword
        Write(">");
        var query = ReadLine();
        if (query == null || query.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
        if (query.IsEmpty()) continue;

        // show result
        var found = await client.SearchAsync(new(query, count: 10), signal.Token);
        if (found.data.Length <= 0)
        {
            WriteLine("No results.");
        }
        else
        {
            WriteLine((found.data.Length < found.total) ? $"Results (first {found.data.Length}):" : "Results:");
            foreach (var item in found.data)
            {
                WriteLine($"  {item.name}: {item.url}");
            }
        }
        WriteLine();
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
