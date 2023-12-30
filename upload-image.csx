#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// File Attachment Sample

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

    // Create book, chapter, and pages.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);

    // Create page
    var book = await client.CreateBookAsync(new("TestBook"), cancelToken: signal.Token);
    var page = await client.CreateMarkdownPageInBookAsync(new(book.id, "TestPage", "# Test page in book"), signal.Token);

    // Upload image
    var image = ContentGenerator.CreateCircleImage(100f, 75f, 50f, format: "png");
    await client.CreateImageAsync(new(page.id, "gallery", "Gallery Image"), image, "img.png");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
