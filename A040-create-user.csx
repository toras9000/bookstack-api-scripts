#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of user creation

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

    // Create client
    using var client = new BookStackClient(info.ApiEntry, info.Key.Token, info.Key.Secret);

    // Create user
    var email = $"user-{Guid.NewGuid()}@example.com";
    var user = await client.CreateUserAsync(new("test-user", email), signal.Token);

    WriteLine("Created user:");
    WriteLine($"  User ID: {user.id}, {user.name} : {settings.ServiceUrl.AuthorityRelative($"settings/users/{user.id}").AbsoluteUri}");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
