#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of shelf creation

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

    // Create books.
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);
    var book1 = await client.CreateBookAsync(new("ApartTestBook1"), cancelToken: signal.Token);
    var book2 = await client.CreateBookAsync(new("ApartTestBook2"), cancelToken: signal.Token);
    var book3 = await client.CreateBookAsync(new("ApartTestBook3"), cancelToken: signal.Token);
    var book4 = await client.CreateBookAsync(new("ApartTestBook4"), cancelToken: signal.Token);
    var book5 = await client.CreateBookAsync(new("ApartTestBook5"), cancelToken: signal.Token);
    var book6 = await client.CreateBookAsync(new("ApartTestBook6"), cancelToken: signal.Token);

    // Create shelves
    var shelf1 = await client.CreateShelfAsync(new("TestShelf1", books: [book1.id, book4.id, book5.id]), cancelToken: signal.Token);
    var shelf2 = await client.CreateShelfAsync(new("TestShelf2", books: [book2.id, book3.id, book4.id, book5.id]), cancelToken: signal.Token);
    var shelf3 = await client.CreateShelfAsync(new("TestShelf3", books: [book1.id]), cancelToken: signal.Token);

    WriteLine("Created contents:");
    WriteLine($"  Shelf1[{shelf1.id}] {Poster.Link[settings.ServiceUrl.AuthorityRelative($"/shelves/{shelf1.slug}").AbsoluteUri]}");
    WriteLine($"  Shelf2[{shelf2.id}] {Poster.Link[settings.ServiceUrl.AuthorityRelative($"/shelves/{shelf2.slug}").AbsoluteUri]}");
    WriteLine($"  Shelf3[{shelf3.id}] {Poster.Link[settings.ServiceUrl.AuthorityRelative($"/shelves/{shelf3.slug}").AbsoluteUri]}");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
