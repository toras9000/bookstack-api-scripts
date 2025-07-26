#load ".common.csx"
#nullable enable
using BookStackApiClient;
using BookStackApiClient.Utility;
using Kokuban;
using Lestaly;

// Export all books as PDFs.

var settings = new
{
    // BookStack service URL.
    ServiceUrl = new Uri("http://localhost:9986/"),

    // Destination directory for export data.
    ExportDir = ThisSource.RelativeDirectory("exports/pdf"),
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

    // List all books
    await foreach (var book in helper.EnumerateAllBooksAsync())
    {
        WriteLine($"Book: {Chalk.Green[book.name]}");
        var pdfBin = await helper.Try((c, breaker) => c.ExportBookPdfAsync(book.id, breaker));
        var pdfFile = settings.ExportDir.RelativeFile($"{book.id:D4}.{book.name.ToFileName()}.pdf").WithDirectoryCreate();
        await pdfFile.WriteAllBytesAsync(pdfBin, signal.Token);
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});