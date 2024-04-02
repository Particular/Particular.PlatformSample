try
{
    await Particular.PlatformLauncher.Launch(showPlatformToolConsoleOutput: false);
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
}