#load ".common.csx"
#nullable enable
using BookStackApiClient;
using Lestaly;

// Sample of update permissions

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

    // Create entities
    var book = await client.CreateBookAsync(new("PermissionsTestBook"), cancelToken: signal.Token);
    var page = await client.CreateMarkdownPageInBookAsync(new(book.id, "PermissionsTestPage", "# Test page in book"), signal.Token);

    // Get role info
    var roles = await client.ListRolesAsync(new(count: 500), signal.Token);
    var roleViewer = roles.data.FirstOrDefault(r => r.display_name == "Viewer") ?? throw new PavedMessageException("Role 'Viewer' not found.");

    // Update permissions
    var permissions = new RolePermission[]
    {
        new(roleViewer.id, view: true, create: true, update: true, delete: false),
    };
    var book_perms = await client.UpdateBookPermissionsAsync(book.id, new(role_permissions: permissions), signal.Token);
    var page_perms = await client.UpdatePagePermissionsAsync(page.id, new(role_permissions: permissions), signal.Token);

    WriteLine("Created contents:");
    WriteLine($"  Book[{book.id}] {settings.ServiceUrl.AuthorityRelative($"books/{book.slug}").AbsoluteUri}");
    WriteLine($"  Page[{page.id}] {settings.ServiceUrl.AuthorityRelative($"books/{book.slug}/page/{page.slug}").AbsoluteUri}");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync();
});
