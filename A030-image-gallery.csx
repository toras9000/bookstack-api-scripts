#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Sample of image upload

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
    var book = await client.CreateBookAsync(new("ImageGalleryTestBook"), cancelToken: signal.Token);
    var page = await client.CreateMarkdownPageInBookAsync(new(book.id, "ImageGalleryTestPage", "# Test page in book"), signal.Token);

    // Upload image
    var file = ThisSource.RelativeFile("materials/images/test-image.png");
    var image1 = await client.CreateImageAsync(new(page.id, "gallery", "Image from file path"), file.FullName, default, signal.Token);

    var binary = ContentGenerator.CreateRectImage(25, 25, 50, 50, imgWidth: 100, imgHeight: 100, format: "png");
    var image2 = await client.CreateImageAsync(new(page.id, "gallery", "Image from binary array"), binary, "gen-image.png", signal.Token);

    // Get information on created object (want URL)
    var foundPage = (await client.SearchAsync(new($"{{type:page}} {{in_name:{page.name}}}"), signal.Token)).data.First(s => s.type == "page" && s.id == page.id);
    ConsoleWig.Write("Uploaded page: ").WriteLink(foundPage.url).NewLine();

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
