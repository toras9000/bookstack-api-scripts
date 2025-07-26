#load "../.common.csx"
#load ".settings.csx"
#nullable enable
using BookStackApiClient;
using BookStackApiClient.Utility;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Show info
    WriteLine($"Delete all books in BookStack.");
    WriteLine($"BookStack Service URL : {settings.BookStack.Url}");

    // Create client and helper
    using var client = new BookStackClient(new(settings.BookStack.Api.Entry), settings.BookStack.Api.TokenId, settings.BookStack.Api.TokenSecret);
    using var helper = new BookStackClientHelper(client, signal.Token);

    // Delete image gallery images
    WriteLine($"Delete Gallery Images");
    while (true)
    {
        // Get a list of images
        var images = await helper.Try((c, breaker) => c.ListImagesAsync(new(count: 500), breaker));
        if (images.data.Length <= 0) break;

        // Delete each image
        foreach (var image in images.data)
        {
            WriteLine($"..  Delete Image [{image.id}] {Chalk.Green[image.name]}");
            await helper.Try((c, breaker) => c.DeleteImageAsync(image.id, breaker));
        }
    }

    // Delete Books
    WriteLine($"Delete Books");
    while (true)
    {
        // Get a list of books
        var books = await helper.Try((c, breaker) => c.ListBooksAsync(new(count: 500), breaker));
        if (books.data.Length <= 0) break;

        // Delete each book
        foreach (var book in books.data)
        {
            WriteLine($"Delete Book [{book.id}] {Chalk.Green[book.name]}");
            await helper.Try((c, breaker) => c.DeleteBookAsync(book.id, breaker));
        }
    }

    // Delete Shelves
    WriteLine($"Delete Shelves");
    while (true)
    {
        // Get a list of shelves
        var shelves = await helper.Try((c, breaker) => c.ListShelvesAsync(new(count: 500), breaker));
        if (shelves.data.Length <= 0) break;

        // Delete each shelf
        foreach (var shelf in shelves.data)
        {
            WriteLine($"Delete Shelf [{shelf.id}] {Chalk.Green[shelf.name]}");
            await helper.Try((c, breaker) => c.DeleteShelfAsync(shelf.id, breaker));
        }
    }

    // Empty the trash
    WriteLine($"Destroy RecycleBin");
    while (true)
    {
        // Get a list of books
        var recycles = await helper.Try((c, breaker) => c.ListRecycleBinAsync(new(count: 500), cancelToken: breaker));
        if (recycles.data.Length <= 0) break;

        // Delete Trash Items
        foreach (var recycle in recycles.data)
        {
            WriteLine($"Destroy {recycle.deletable_type} [{recycle.id}]");
            await helper.Try((c, breaker) => c.DestroyRecycleItemAsync(recycle.id, cancelToken: breaker));
        }
    }

    WriteLine($"Completed");
});

