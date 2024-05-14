try
{
    await Particular.PlatformLauncher.Launch(showPlatformToolConsoleOutput: true);
}
catch (Exception e)
{
    Console.WriteLine(e);
    Console.ReadLine();
}