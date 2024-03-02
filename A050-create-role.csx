#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using BookStackApiClient;
using Lestaly;

// Sample of role creation

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

    ConsoleWig.WriteLine("Created role:");
    ConsoleWig.Write($"  Role ID={role.id}, {role.display_name} ").WriteLink(settings.ServiceUrl.AuthorityRelative($"settings/roles/{role.id}").AbsoluteUri).NewLine();

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
