#r "nuget: Lestaly, 0.48.0"
#nullable enable
using System.Threading;
using Lestaly;
using Lestaly.Cx;

// Restart docker container with deletion of persistent data.
// (If it is not activated, it is simply activated.)

await Paved.RunAsync(async () =>
{
    try
    {
        var composeFile = ThisSource.RelativeFile("./docker/docker-compose.yml");
        Console.WriteLine("Restart service");
        await "docker".args("compose", "--file", composeFile.FullName, "down", "--remove-orphans", "--volumes");
        await "docker".args("compose", "--file", composeFile.FullName, "up", "-d").result().success();
        Console.WriteLine("completed.");
    }
    catch (CmdProcExitCodeException err)
    {
        throw new PavedMessageException($"ExitCode: {err.ExitCode}\nOutput: {err.Output}", err);
    }
});
