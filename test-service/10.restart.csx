#r "nuget: Lestaly.General, 0.100.0"
#load ".settings.csx"
#nullable enable
using System.Threading;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    WriteLine("Restart service");
    await "docker".args("compose", "--file", settings.Docker.Compose, "down", "--remove-orphans", "--volumes").echo();
    await "docker".args("compose", "--file", settings.Docker.Compose, "up", "-d", "--wait").echo().result().success();

    WriteLine();
    await "dotnet".args("script", ThisSource.RelativeFile("12.meke-api-token.csx"), "--", "--no-pause").result().success();

    WriteLine();
    WriteLine($"Service URL");
    WriteLine($"  {Poster.Link[settings.BookStack.Url]}");
});
