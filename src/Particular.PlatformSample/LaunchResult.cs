namespace Particular
{
    /// <summary>
    /// Describes why the platform launcher stopped.
    /// </summary>
    public sealed class LaunchResult
    {
        internal LaunchResult(ProcessExit unexpectedExit = null) => UnexpectedExit = unexpectedExit;

        /// <summary>
        /// The tool that exited unexpectedly, or null when shutdown was requested through Ctrl+C or the cancellation token.
        /// </summary>
        public ProcessExit UnexpectedExit { get; }
    }
}
