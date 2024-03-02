#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Sample of various content creation

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
    var book = await client.CreateBookAsync(new("CreateTestBook", tags: [new("tag1"), new("tag2")]), cancelToken: signal.Token);
    var chapter = await client.CreateChapterAsync(new(book.id, "CreateTestChapter"), signal.Token);
    var page1 = await client.CreateMarkdownPageInBookAsync(new(book.id, "CreateTestPageInBook", "# Test page in book"), signal.Token);
    var page2 = await client.CreateMarkdownPageInChapterAsync(new(chapter.id, "CreateTestPageInChapter", "# Test page in chapter"), signal.Token);

    // Get information on created object (want URL)
    var searchBooks = await client.SearchAsync(new($"{{type:book}} {{in_name:{book.name}}}"), signal.Token);
    var foundBook = searchBooks.data.First(s => s.type == "book" && s.id == book.id);
    var detailBook = await client.ReadBookAsync(book.id, signal.Token);
    var contentChapter = detailBook.chapters().First(c => c.id == chapter.id);
    var contentPage1 = detailBook.pages().First(p => p.id == page1.id);
    var contentPage2 = detailBook.chapters().SelectMany(c => c.pages.CoalesceEmpty()).First(p => p.id == page2.id);

    ConsoleWig.WriteLine("Created contents:");
    ConsoleWig.Write($"  Book[{book.id}] ").WriteLink(foundBook.url).NewLine();
    ConsoleWig.Write($"  Chapter[{chapter.id}] ").WriteLink(contentChapter.url).NewLine();
    ConsoleWig.Write($"  Page1[{page1.id}] ").WriteLink(contentPage1.url).NewLine();
    ConsoleWig.Write($"  Page2[{page2.id}] ").WriteLink(contentPage2.url).NewLine();

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
