#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// File Attachment Sample

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
    using var signal = new CancellationTokenSource();
    using var handler = ConsoleWig.CancelKeyHandlePeriod(signal);

    // Show access address
    ConsoleWig.WriteLine($"API entrypoint : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Create book, chapter, and pages.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var book = await client.CreateBookAsync(new("TestBook", tags: new Tag[] { new("test") }), cancelToken: signal.Token);
    var page = await client.CreateMarkdownPageInBookAsync(new(book.id, "TestPage", "# Test page in book"), signal.Token);

    // attach
    var file = ThisSource.GetFile();
    var attach1 = await client.CreateFileAttachmentAsync(new("attach from path", page.id), file.FullName, default, signal.Token);

    var content = Encoding.UTF8.GetBytes("attachment test");
    var attach2 = await client.CreateFileAttachmentAsync(new("attach from array", page.id), content, "test.txt", signal.Token);

    // Get information on created object (want URL)
    var foundPage = (await client.SearchAsync(new($"{{type:page}} {{in_name:{page.name}}}"), signal.Token))
        .data.First(s => s.type == "page" && s.id == page.id);
    ConsoleWig.Write("Attached page: ").WriteLink(foundPage.url).WriteLine("");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
