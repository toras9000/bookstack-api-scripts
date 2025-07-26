#load ".common.csx"
#nullable enable
using BookStackApiClient;
using BookStackApiClient.Utility;
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
    using var helper = new BookStackClientHelper(client, signal.Token);
    helper.LimitHandler += async a => await Task.Delay(TimeSpan.FromSeconds(a.Exception.RetryAfter));

    // Show audit logs
    await foreach (var log in helper.EnumerateAllAuditLogsAsync())
    {
        WriteLine($"{log.created_at.ToLocalTime()}: {log.ip} {log.user?.name} {log.type} {log.loggable_type.WhenEmpty("*")} - {log.detail}");
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});