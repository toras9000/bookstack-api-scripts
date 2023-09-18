#r "nuget: Lestaly, 0.47.0"
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
        Console.WriteLine("Stop service");
        await "docker".args("compose", "--file", composeFile.FullName, "down", "--remove-orphans", "--volumes");
        Console.WriteLine("Start service");
        await "docker".args("compose", "--file", composeFile.FullName, "up", "-d").AsSuccessCode();
        Console.WriteLine("completed.");
    }
    catch (CmdProcExitCodeException err)
    {
        throw new PavedMessageException($"ExitCode: {err.ExitCode}\nOutput: {err.Output}", err);
    }
});
