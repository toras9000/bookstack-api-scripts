#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;
using SkiaSharp;

// Create a variety of appropriate content for testing.

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

    // Generate a number of entities for testing.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);

    // generate test image
    var images = new List<(byte[] bin, string ext)>
    {
        (ContentGenerator.CreateCircleImage(100f, 75f, 50f, format: "png"),  "png"),
        (ContentGenerator.CreateCircleImage(100f, 75f, 50f, format: "jpeg"), "jpg"),
    };

    var guid = Guid.NewGuid().ToString();
    for (var b = 0; b < 5; b++)
    {
        Console.WriteLine($"Create Book{b:D3} ...");
        var book = await helper.Try(c => c.CreateBookAsync(new($"TestBook {guid} B{b:D3}", $"Desctiption Book{b:D3}"), cancelToken: signal.Token));
        for (var c = 0; c < 3; c++)
        {
            var chapter = await helper.Try(c => c.CreateChapterAsync(new(book.id, $"TestChapter {guid} B{b:D3} C{c:D3}", $"B{b:D3}-Chapter{c:D3}"), signal.Token));
            for (var p = 0; p < 4; p++)
            {
                var page = p switch
                {
                    < 2 => await helper.Try(c => c.CreateMarkdownPageInChapterAsync(new(chapter.id, $"TestPage in Chapter {guid} B{b:D3} C{c:D3} CP{p:D3}", $"markdown in chapter B{b:D3}-C{c:D3}-CP{p:D3}"), signal.Token)),
                    _ => await helper.Try(c => c.CreateHtmlPageInChapterAsync(new(chapter.id, $"TestPage in Chapter {guid} B{b:D3} C{c:D3} CP{p:D3}", $"html in chapter B{b:D3}-C{c:D3}-CP{p:D3}"), signal.Token)),
                };

                for (var a = 0; a < 2; a++)
                {
                    var attach = p switch
                    {
                        0 or 2 => await helper.Try(c => c.CreateFileAttachmentAsync(new($"file{a}", page.id), $"TestAttach in {guid} B{b:D3} C{c:D3} CP{p:D3}".EncodeUtf8(), $"file{p}.txt", signal.Token)),
                        _ => await helper.Try(c => c.CreateLinkAttachmentAsync(new($"link{a}", page.id, $"https://example.com/{a}"), signal.Token)),
                    };

                    var image = p switch
                    {
                        1 or 2 => await helper.Try(c => c.CreateImageAsync(new(page.id, "gallery", $"test-image-{a}"), images[a].bin, $"image.{images[a].ext}", signal.Token)),
                        _ => await Task.FromResult<ImageItem>(default!),
                    };
                }
            }
        }

        for (var p = 0; p < 4; p++)
        {
            var page = b switch
            {
                < 2 => await helper.Try(c => c.CreateMarkdownPageInBookAsync(new(book.id, $"TestPage in Book {guid} B{b:D3} BP{p:D3}", $"markdown in book B{b:D3}-BP{p:D3}"), signal.Token)),
                _ => await helper.Try(c => c.CreateHtmlPageInBookAsync(new(book.id, $"TestPage in Book {guid} B{b:D3} BP{p:D3}", $"html in book B{b:D3}-BP{p:D3}"), signal.Token)),
            };

            for (var a = 0; a < 2; a++)
            {
                var attach = p switch
                {
                    0 or 1 => await helper.Try(c => c.CreateLinkAttachmentAsync(new($"link{a}", page.id, $"https://example.com/{a}"), signal.Token)),
                    _ => await helper.Try(c => c.CreateFileAttachmentAsync(new($"file{a}", page.id), $"TestAttach in {guid} B{b:D3} BP{p:D3}".EncodeUtf8(), $"file{a}.txt", signal.Token)),
                };

                var image = p switch
                {
                    0 or 3 => await helper.Try(c => c.CreateImageAsync(new(page.id, "gallery", $"test-image-{a}"), images[a].bin, $"image.{images[a].ext}", signal.Token)),
                    _ => await Task.FromResult<ImageItem>(default!),
                };
            }
        }
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
