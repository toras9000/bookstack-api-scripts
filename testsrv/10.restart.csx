// This script is meant to run with dotnet-script.
// You can install .NET SDK 6.0 and install dotnet-script with the following command.
// $ dotnet tool install -g dotnet-script

#r "nuget: Lestaly, 0.45.0"
using System.Threading;
using Lestaly;

// Restart docker container with deletion of persistent data.
// (If it is not activated, it is simply activated.)

await Paved.RunAsync(async () =>
{
    try
    {
        var composeFile = ThisSource.RelativeFile("./docker/docker-compose.yml");
        Console.WriteLine("Stop service");
        await CmdProc.CallAsync("docker", new[] { "compose", "--file", composeFile.FullName, "down", "--remove-orphans", "--volumes", });
        Console.WriteLine("Start service");
        await CmdProc.CallAsync("docker", new[] { "compose", "--file", composeFile.FullName, "up", "-d", });
        Console.WriteLine("completed.");
    }
    catch (CmdProcExitCodeException err)
    {
        throw new PavedMessageException($"ExitCode: {err.ExitCode}\nOutput: {err.Output}", err);
    }
});
