namespace Particular
{
    sealed record ProcessExit(string ProcessName, int ExitCode, string StandardOutput, string StandardError);
}
