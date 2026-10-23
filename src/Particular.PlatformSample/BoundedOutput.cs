namespace Particular
{
    using System;
    using System.Text;

    sealed class BoundedOutput
    {
        const int MaximumLength = 8192;
        readonly StringBuilder output = new(MaximumLength);

        public void AppendLine(string line)
        {
            lock (output)
            {
                Append(line.AsSpan());
                Append(Environment.NewLine.AsSpan());
            }
        }

        void Append(ReadOnlySpan<char> text)
        {
            if (text.Length >= MaximumLength)
            {
                output.Clear();
                output.Append(text[^MaximumLength..]);
                return;
            }

            var excess = output.Length + text.Length - MaximumLength;
            if (excess > 0)
            {
                output.Remove(0, excess);
            }

            output.Append(text);
        }

        public override string ToString()
        {
            lock (output)
            {
                return output.ToString();
            }
        }
    }
}
