#load "../.common.csx"
#load ".settings.csx"
#nullable enable
using System.Net.Http;
using BookStackApiClient;
using BookStackApiClient.Utility;
using Lestaly;
using R3;
using SkiaSharp;

return await Paved.ProceedAsync(async () =>
{
    // Number of objects to be generated.
    var genBooks = new { min = 2, max = 5, };
    var genContents = new { min = 3, max = 6, };
    var genSubPages = new { min = 2, max = 4, };
    var genPageImages = new { min = 2, max = 3, };
    var genPageAttaches = new { min = 2, max = 3, };

    // Force option
    var forceGenerate = false;

    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Show info
    WriteLine($"Create dummy data in BookStack.");
    WriteLine($"BookStack Service URL : {settings.BookStack.Url}");

    // Create client and helper
    using var client = new BookStackClient(new(settings.BookStack.Api.Entry), settings.BookStack.Api.TokenId, settings.BookStack.Api.TokenSecret);
    using var helper = new BookStackClientHelper(client, signal.Token);

    // If not forced, check the status.
    if (!forceGenerate)
    {
        var books = await helper.Try((c, breaker) => c.ListBooksAsync(cancelToken: breaker));
        if (0 < books.total)
        {
            throw new PavedMessageException($"Some kind of book already exists.", PavedMessageKind.Warning);
        }
    }

    // Create a dummy objects.
    var bookCount = Random.Shared.Next(genBooks.min, genBooks.max + 1);
    for (var b = 0; b < bookCount; b++)
    {
        var bookNum = 1 + b;
        WriteLine($"Create dummy Book {bookNum} ...");
        var bookCover = ContentGenerator.CreateTextImage($"Book {bookNum} Cover");
        var book = await helper.Try((c, breaker) => c.CreateBookAsync(new($"Book {bookNum}", $"Generated {DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}"), bookCover, $"cover.png", cancelToken: breaker));
        var contentCount = Random.Shared.Next(genContents.min, genContents.max + 1);
        for (var c = 0; c < contentCount; c++)
        {
            var contentNum = 1 + c;
            if (Random.Shared.Next(2) == 0)
            {
                var pageLabel = $"B{bookNum}-P{contentNum}";
                WriteLine($"  Create dummy content {contentNum} Page ...");
                var page = Random.Shared.Next(2) switch
                {
                    0 => await helper.Try((c, breaker) => c.CreateMarkdownPageInBookAsync(new(book.id, $"Page {pageLabel}", $"Generated {DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}"), breaker)),
                    _ => await helper.Try((c, breaker) => c.CreateHtmlPageInBookAsync(new(book.id, $"Page {pageLabel}", $"<span>Generated <time>{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}</time></span>"), breaker)),
                };

                await createPageMaterials(page, pageLabel, $"B{bookNum}/P{contentNum}");
            }
            else
            {
                WriteLine($"  Create dummy content {contentNum} Chapter ...");
                var chapterLabel = $"B{bookNum}-C{contentNum}";
                var chapter = await helper.Try((c, breaker) => c.CreateChapterAsync(new(book.id, $"Chapter {chapterLabel}", $"Generated {DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}"), breaker));
                var pageCount = Random.Shared.Next(genSubPages.min, genSubPages.max + 1);
                for (var p = 0; p < pageCount; p++)
                {
                    var pageNum = 1 + p;
                    var pageLabel = $"B{bookNum}-C{contentNum}-P{pageNum}";
                    WriteLine($"    Create dummy page {pageLabel} and materials ...");
                    var page = Random.Shared.Next(2) switch
                    {
                        0 => await helper.Try((c, breaker) => c.CreateMarkdownPageInChapterAsync(new(chapter.id, $"Page {pageLabel}", $"Generated {DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}"), breaker)),
                        _ => await helper.Try((c, breaker) => c.CreateHtmlPageInChapterAsync(new(chapter.id, $"Page {pageLabel}", $"<span>Generated <time>{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}</time></span>"), breaker)),
                    };

                    await createPageMaterials(page, pageLabel, $"B{bookNum}/C{contentNum}/P{pageNum}");
                }
            }

            async ValueTask createPageMaterials(PageItem page, string pageLabel, string linkPath)
            {
                var imageCount = Random.Shared.Next(genPageImages.min, genPageImages.max + 1);
                for (var i = 0; i < imageCount; i++)
                {
                    var imageLabel = $"{pageLabel}-{i}";
                    var imageBin = ContentGenerator.CreateTextImage(imageLabel);
                    var image = await helper.Try((c, breaker) => c.CreateImageAsync(new(page.id, "gallery", $"Image-{imageLabel}"), imageBin, $"{imageLabel}.png", breaker));
                }

                var attachCount = Random.Shared.Next(genPageAttaches.min, genPageAttaches.max + 1);
                for (var a = 0; a < attachCount; a++)
                {
                    var attachLabel = $"{pageLabel}-{a}";
                    if (Random.Shared.Next(2) == 0)
                    {
                        var attachBin = Encoding.UTF8.GetBytes($"TextContent-{attachLabel}");
                        var attach = await helper.Try((c, breaker) => c.CreateFileAttachmentAsync(new($"Text-{attachLabel}", page.id), attachBin, $"{attachLabel}.txt", breaker));
                    }
                    else
                    {
                        var attachLink = $"http://localhost/{linkPath}/{a}";
                        var attach = await helper.Try((c, breaker) => c.CreateLinkAttachmentAsync(new($"Text-{attachLabel}", page.id, attachLink), breaker));
                    }
                }
            }

        }
    }

    WriteLine($"Completed");
});
