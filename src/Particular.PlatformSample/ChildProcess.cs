namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    sealed class ChildProcess : IAsyncDisposable
    {
        const int MaximumOutputLength = 8192;
        readonly Process process;

        public ChildProcess(string name, ProcessStartInfo startInfo, bool showConsoleOutput)
        {
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start platform tool '{name}'.");

            var output = ReadOutput(process.StandardOutput, showConsoleOutput ? Console.Out : null, CancellationToken.None);
            var error = ReadOutput(process.StandardError, showConsoleOutput ? Console.Error : null, CancellationToken.None);
            Exited = ObserveExit(name, output, error, CancellationToken.None);
        }

        public Task<ProcessExit> Exited { get; }

        async Task<ProcessExit> ObserveExit(string name, Task<string> output, Task<string> error, CancellationToken cancellationToken)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                var capturedOutput = await Task.WhenAll(output, error).ConfigureAwait(false);

                return new ProcessExit(name, process.ExitCode, capturedOutput[0], capturedOutput[1]);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Could not monitor platform tool '{name}'.", exception);
            }
        }

        static async Task<string> ReadOutput(StreamReader reader, TextWriter console, CancellationToken cancellationToken)
        {
            var output = new StringBuilder();
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                output.AppendLine(line);
                if (output.Length > MaximumOutputLength)
                {
                    output.Remove(0, output.Length - MaximumOutputLength);
                }

                if (console != null)
                {
                    await console.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
                }
            }

            return output.ToString();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                try
                {
                    // Ctrl+C also reaches the tools. Let them finish shutting down before forcing termination.
                    await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill(entireProcessTree: true);
                        }
                        catch (InvalidOperationException) when (process.HasExited)
                        {
                            // The tool can exit between checking HasExited and requesting termination.
                        }
                    }
                }

                await Exited.ConfigureAwait(false);
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
