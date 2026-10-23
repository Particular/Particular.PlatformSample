namespace Particular
{
    /// <summary>
    /// The exit status and recent console output of a platform tool.
    /// </summary>
    public sealed class ProcessExit
    {
        internal ProcessExit(string processName, int exitCode, string standardOutput, string standardError)
        {
            ProcessName = processName;
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        /// <summary>The name of the platform tool.</summary>
        public string ProcessName { get; }

        /// <summary>The process exit code. Even zero is unexpected while the launcher is running.</summary>
        public int ExitCode { get; }

        /// <summary>The most recent standard output, truncated to the last 8192 characters.</summary>
        public string StandardOutput { get; }

        /// <summary>The most recent standard error output, truncated to the last 8192 characters.</summary>
        public string StandardError { get; }
    }
}
