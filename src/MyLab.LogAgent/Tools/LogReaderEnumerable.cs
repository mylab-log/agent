using System.Collections;

namespace MyLab.LogAgent.Tools;

class LogReaderEnumerable(StreamReader streamReader, IEnumerable<string>? leaderLines) : IAsyncEnumerable<string>
{
    private readonly StreamReader _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));

    public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new LogReaderEnumerator(_streamReader, leaderLines, cancellationToken);
    }

    class LogReaderEnumerator(StreamReader streamReader, IEnumerable<string>? leaderLines, CancellationToken cancellationToken = default) : IAsyncEnumerator<string>
    {
        private bool _readFromStream = leaderLines == null;
        private readonly IEnumerator<string>? _leaderLinesEnumerator = leaderLines?.GetEnumerator();
        
        public async ValueTask<bool> MoveNextAsync()
        {
            if (!_readFromStream)
            {
                if (!_leaderLinesEnumerator!.MoveNext())
                {
                    _readFromStream = true;
                }
                else
                {
                    Current = _leaderLinesEnumerator.Current;
                    return true;
                }
            }

            var readLine = await streamReader.ReadLineAsync(cancellationToken);

            if (readLine == null) return false;

            Current = readLine;
            return true;
        }

        public string Current { get; private set; } = String.Empty;
        
        public ValueTask DisposeAsync()
        {
            _readFromStream = leaderLines == null;
            return ValueTask.CompletedTask;
        }
    }
}