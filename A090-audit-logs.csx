#load ".common.csx"
#nullable enable
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
return await Paved.ProceedAsync(async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Show access address
    WriteLine($"Service URL : {settings.ServiceUrl}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(new(settings.ServiceUrl, "/api/"), signal.Token);

    // Create client and helper
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client, signal.Token);

    // List all logs
    var offset = 0;
    while (true)
    {
        // Get a list of audit-logs
        var logs = await helper.Try(c => c.ListAuditLogAsync(new(offset, count: 500), signal.Token));
        if (logs.data.Length <= 0) break;

        // Show audit logs
        var userWidth = logs.data.Max(l => l.user?.name?.Length ?? 0);
        var eventWidth = logs.data.Max(l => l.type?.Length ?? 0);
        foreach (var log in logs.data)
        {
            var name = (log.user?.name ?? "").Decorate(n => $"[{n}]").PadRight(userWidth);
            var logtype = (log.type ?? "").PadRight(eventWidth);
            WriteLine($"{log.created_at.ToLocalTime()}: {log.ip} {name} {logtype} {log.loggable_type.WhenEmpty("*")} - {log.detail}");
        }

        // Update search information and determine end of search.
        offset += logs.data.Length;
        var finished = (logs.data.Length <= 0) || (logs.total <= offset);
        if (finished) break;
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});