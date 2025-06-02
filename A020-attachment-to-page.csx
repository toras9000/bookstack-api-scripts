#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of attachment

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

    // Create book, chapter, and pages.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var book = await client.CreateBookAsync(new("AttachmentTestBook"), cancelToken: signal.Token);
    var page = await client.CreateMarkdownPageInBookAsync(new(book.id, "AttachmentTestPage", "# Test page in book"), signal.Token);

    // Attach
    var file = ThisSource.File();
    var attach1 = await client.CreateFileAttachmentAsync(new("attach from path", page.id), file.FullName, default, signal.Token);

    var content = Encoding.UTF8.GetBytes("attachment test");
    var attach2 = await client.CreateFileAttachmentAsync(new("attach from array", page.id), content, "test.txt", signal.Token);

    var url = "http://localhost:9986/";
    var attach3 = await client.CreateLinkAttachmentAsync(new("attach link", page.id, url), signal.Token);

    // Get information on created object (want URL)
    var foundPage = (await client.SearchAsync(new($"{{type:page}} {{in_name:{page.name}}}"), signal.Token)).data.First(s => s.type == "page" && s.id == page.id);
    WriteLine($"Attached page: {Poster.Link[foundPage.url]}");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
