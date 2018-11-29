namespace SmokeTest
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Particular.PlatformLauncher.Launch();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }
    }
}
