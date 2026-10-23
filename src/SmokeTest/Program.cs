try
{
    var result = await Particular.PlatformLauncher.LaunchWithResult(showPlatformToolConsoleOutput: true);
    return result.UnexpectedExit == null ? 0 : 1;
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
    return 1;
}
