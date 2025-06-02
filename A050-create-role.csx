#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of role creation

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

    // Create role
    var permissions = new[]
    {
        RolePermissions.CreateAllBooks,
        RolePermissions.CreateAllBookShelves,
        RolePermissions.CreateAllChapters,
        RolePermissions.CreateAllPages,
    };
    var role = await client.CreateRoleAsync(new("test-role", permissions: permissions), signal.Token);

    WriteLine("Created role:");
    WriteLine($"  Role ID={role.id}, {role.display_name} : {settings.ServiceUrl.AuthorityRelative($"settings/roles/{role.id}").AbsoluteUri}");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
