namespace SmokeTest
{
    using System;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main()
        {
            try
            {
                await Particular.PlatformLauncher.Launch(showPlatformToolConsoleOutput: false)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }
    }
}