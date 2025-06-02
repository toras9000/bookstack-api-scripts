#load ".common.csx"
#nullable enable
using BookStackApiClient;
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
    var helper = new BookStackClientHelper(client, signal.Token);

    // List all books
    var offset = 0;
    while (true)
    {
        // Get a list of books
        var books = await helper.Try(c => c.ListBooksAsync(new(offset, count: 500), signal.Token));
        if (books.data.Length <= 0) break;

        // Export books
        foreach (var book in books.data)
        {
            WriteLine($"Book: {Chalk.Green[book.name]}");
            var pdfBin = await helper.Try(c => c.ExportBookPdfAsync(book.id, signal.Token));
            var pdfFile = settings.ExportDir.RelativeFile($"{book.id:D4}.{book.name.ToFileName()}.pdf").WithDirectoryCreate();
            await pdfFile.WriteAllBytesAsync(pdfBin, signal.Token);
        }

        // Update search information and determine end of search.
        offset += books.data.Length;
        var finished = (books.data.Length <= 0) || (books.total <= offset);
        if (finished) break;
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});