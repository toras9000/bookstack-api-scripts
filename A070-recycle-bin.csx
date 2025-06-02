#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of recycle bin

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
    var book = await client.CreateBookAsync(new("RecycleBinTestBook", tags: [new("tag1"), new("tag2")]), cancelToken: signal.Token);
    var chapter = await client.CreateChapterAsync(new(book.id, "RecycleBinTestChapter"), signal.Token);
    var page1 = await client.CreateMarkdownPageInBookAsync(new(book.id, "RecycleBinTestPageInBook", "# Test page in book"), signal.Token);
    var page2 = await client.CreateMarkdownPageInChapterAsync(new(chapter.id, "RecycleBinTestPageInChapter", "# Test page in chapter"), signal.Token);

    // Delete entities
    await client.DeletePageAsync(page1.id, signal.Token);
    await client.DeletePageAsync(page2.id, signal.Token);
    await client.DeleteChapterAsync(chapter.id, signal.Token);
    await client.DeleteBookAsync(book.id, signal.Token);

    // Get recycle bin list
    var trashes = await client.ListRecycleBinAsync(default, signal.Token);
    var show = 10;
    WriteLine($"Recycle bin{(show < trashes.data.Length ? $" (first {show})" : "")}:");
    foreach (var trash in trashes.data.Take(show))
    {
        WriteLine($"  Deletable {trash.deletable_type}: {trash.deletable.name}");
    }

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
