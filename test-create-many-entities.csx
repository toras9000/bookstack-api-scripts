#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Create a variety of appropriate content for testing.

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
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Show access address
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Generate a number of entities for testing.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var helper = new BookStackClientHelper(client);

    var guid = Guid.NewGuid().ToString();
    for (var b = 0; b < 5; b++)
    {
        Console.WriteLine($"Create Book{b:D3} ...");
        var book = await helper.Try(c => c.CreateBookAsync(new($"TestBook {guid} B{b:D3}", $"Desctiption Book{b:D3}"), cancelToken: signal.Token));
        for (var c = 0; c < 3; c++)
        {
            var chapter = await helper.Try(c => c.CreateChapterAsync(new(book.id, $"TestChapter {guid} B{b:D3} C{c:D3}", $"B{b:D3}-Chapter{c:D3}"), signal.Token));
            for (var p = 0; p < 2; p++)
            {
                var page = await helper.Try(c => c.CreateMarkdownPageInChapterAsync(new(chapter.id, $"TestPage in Chapter {guid} B{b:D3} C{c:D3} CP{p:D3}", $"markdown in chapter B{b:D3}-C{c:D3}-CP{p:D3}"), signal.Token));
                for (var a = 0; a < 2; a++)
                {
                    var attach = Encoding.UTF8.GetBytes($"TestAttach in {guid} B{b:D3} C{c:D3} CP{p:D3}");
                    await helper.Try(c => c.CreateFileAttachmentAsync(new($"file{a}", page.id), attach, $"file{a}.txt", signal.Token));
                }
            }
            for (var p = 2; p < 4; p++)
            {
                var page = await helper.Try(c => c.CreateHtmlPageInChapterAsync(new(chapter.id, $"TestPage in Chapter {guid} B{b:D3} C{c:D3} CP{p:D3}", $"html in chapter B{b:D3}-C{c:D3}-CP{p:D3}"), signal.Token));
                for (var a = 0; a < 2; a++)
                {
                    await helper.Try(c => c.CreateLinkAttachmentAsync(new($"link{a}", page.id, $"https://example.com/{a}"), signal.Token));
                }
            }
        }
        for (var p = 0; p < 2; p++)
        {
            var page = await helper.Try(c => c.CreateMarkdownPageInBookAsync(new(book.id, $"TestPage in Book {guid} B{b:D3} BP{p:D3}", $"markdown in book B{b:D3}-BP{p:D3}"), signal.Token));
            for (var a = 0; a < 2; a++)
            {
                await helper.Try(c => c.CreateLinkAttachmentAsync(new($"link{a}", page.id, $"https://example.com/{a}"), signal.Token));
            }
        }
        for (var p = 2; p < 4; p++)
        {
            var page = await helper.Try(c => c.CreateHtmlPageInBookAsync(new(book.id, $"TestPage in Book {guid} B{b:D3} BP{p:D3}", $"html in book B{b:D3}-BP{p:D3}"), signal.Token));
            for (var a = 0; a < 2; a++)
            {
                var attach = Encoding.UTF8.GetBytes($"TestAttach in {guid} B{b:D3} BP{p:D3}");
                await helper.Try(c => c.CreateFileAttachmentAsync(new($"file{a}", page.id), attach, $"file{a}.txt", signal.Token));
            }
        }
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
