#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Samples of various content creation

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
    var chapter = await client.CreateChapterAsync(new(book.id, "TestChapter"), signal.Token);
    var page1 = await client.CreateMarkdownPageInBookAsync(new(book.id, "TestPage", "# Test page in book"), signal.Token);
    var page2 = await client.CreateMarkdownPageInChapterAsync(new(chapter.id, "TestPage", "# Test page in chapter"), signal.Token);

    // Get information on created object (want URL)
    var searchBooks = await client.SearchAsync(new($"{{type:book}} {{in_name:{book.name}}}"), signal.Token);
    var foundBook = searchBooks.data.First(s => s.type == "book" && s.id == book.id);
    var detailBook = await client.ReadBookAsync(book.id, signal.Token);
    var contentChapter = detailBook.chapters().First(c => c.id == chapter.id);
    var contentPage1 = detailBook.pages().First(p => p.id == page1.id);
    var contentPage2 = detailBook.chapters().SelectMany(c => c.pages.CoalesceEmpty()).First(p => p.id == page2.id);

    ConsoleWig.WriteLine("Created contents:");
    ConsoleWig.Write($"  Book[{book.id}] ").WriteLink(foundBook.url).WriteLine("");
    ConsoleWig.Write($"  Chapter[{chapter.id}] ").WriteLink(contentChapter.url).WriteLine("");
    ConsoleWig.Write($"  Page1[{page1.id}] ").WriteLink(contentPage1.url).WriteLine("");
    ConsoleWig.Write($"  Page2[{page2.id}] ").WriteLink(contentPage2.url).WriteLine("");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
