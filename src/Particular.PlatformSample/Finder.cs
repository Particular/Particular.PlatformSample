namespace Particular
{
    using System;
    using System.IO;
    using System.Linq;

    class Finder
    {
        public string SolutionRoot { get; }

        public Finder()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            while (true)
            {
                if (Directory.EnumerateFiles(directory).Any(file => file.EndsWith(".sln")))
                {
                    SolutionRoot = directory;
                    return;
                }

                var parent = Directory.GetParent(directory);

                if (parent == null)
                {
                    // throw for now. if we discover there is an edge then we can fix it in a patch.
                    throw new Exception("Couldn't find the solution directory for the learning transport. If the endpoint is outside the solution folder structure, make sure to specify a storage directory using the 'EndpointConfiguration.UseTransport<LearningTransport>().StorageDirectory()' API.");
                }

                directory = parent.FullName;
            }
        }

        public string GetDirectory(string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(SolutionRoot, relativePath));
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }
    }
}