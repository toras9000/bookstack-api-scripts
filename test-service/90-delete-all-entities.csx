#load "../.common.csx"
#nullable enable
using System.Net.Http;
using System.Threading;
using System.Xml.Linq;
using BookStackApiClient;
using Kokuban;
using Lestaly;

await Paved.RunAsync(async () =>
{
    // BookStack service URL.
    var serviceUri = new Uri("http://localhost:9986/");

    // API Token and Secret Key
    var apiToken = "00001111222233334444555566667777";
    var apiSecret = "88889999aaaabbbbccccddddeeeeffff";

    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Show info
    Console.WriteLine($"Delete all books in BookStack.");
    Console.WriteLine($"BookStack Service URL : {serviceUri}");

    // Create client and helper
    using var client = new BookStackClient(new(serviceUri, "/api/"), apiToken, apiSecret);
    var helper = new BookStackClientHelper(client, signal.Token);

    // Delete image gallery images
    Console.WriteLine($"Delete Gallery Images");
    while (true)
    {
        // Get a list of images
        var images = await helper.Try(s => s.ListImagesAsync(new(count: 500), signal.Token));
        if (images.data.Length <= 0) break;

        // Delete each image
        foreach (var image in images.data)
        {
            Console.WriteLine($"..  Delete Image [{image.id}] {Chalk.Green[image.name]}");
            await helper.Try(s => s.DeleteImageAsync(image.id, signal.Token));
        }
    }

    // Delete Books
    Console.WriteLine($"Delete Books");
    while (true)
    {
        // Get a list of books
        var books = await helper.Try(s => s.ListBooksAsync(new(count: 500), signal.Token));
        if (books.data.Length <= 0) break;

        // Delete each book
        foreach (var book in books.data)
        {
            Console.WriteLine($"Delete Book [{book.id}] {Chalk.Green[book.name]}");
            await helper.Try(s => s.DeleteBookAsync(book.id, signal.Token));
        }
    }

    // Delete Shelves
    Console.WriteLine($"Delete Shelves");
    while (true)
    {
        // Get a list of shelves
        var shelves = await helper.Try(s => s.ListShelvesAsync(new(count: 500), signal.Token));
        if (shelves.data.Length <= 0) break;

        // Delete each shelf
        foreach (var shelf in shelves.data)
        {
            Console.WriteLine($"Delete Shelf [{shelf.id}] {Chalk.Green[shelf.name]}");
            await helper.Try(s => s.DeleteShelfAsync(shelf.id, signal.Token));
        }
    }

    // Empty the trash
    Console.WriteLine($"Destroy RecycleBin");
    while (true)
    {
        // Get a list of books
        var recycles = await helper.Try(s => s.ListRecycleBinAsync(new(count: 500), cancelToken: signal.Token));
        if (recycles.data.Length <= 0) break;

        // Delete Trash Items
        foreach (var recycle in recycles.data)
        {
            Console.WriteLine($"Destroy {recycle.deletable_type} [{recycle.id}]");
            await helper.Try(s => s.DestroyRecycleItemAsync(recycle.id, cancelToken: signal.Token));
        }
    }

    Console.WriteLine($"Completed");
});

