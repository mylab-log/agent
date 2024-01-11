using MyLab.LogAgent.LogSourceReaders;
using System.Collections;

namespace MyLab.LogAgent.Tools;

class LogReaderEnumerable : IAsyncEnumerable<LogSourceLine?>
{
    private readonly ILogSourceReader _logSourceReader;
    private readonly IEnumerable<LogSourceLine>? _leaderLines;

    public LogReaderEnumerable(ILogSourceReader logSourceReader, IEnumerable<LogSourceLine>? leaderLines)
    {
        _logSourceReader = logSourceReader;
        _leaderLines = leaderLines;
    }

    public IAsyncEnumerator<LogSourceLine?> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new LogReaderEnumerator(_logSourceReader, _leaderLines, cancellationToken);
    }

    class LogReaderEnumerator(ILogSourceReader logSourceReader, IEnumerable<LogSourceLine>? leaderLines, CancellationToken cancellationToken = default) : IAsyncEnumerator<LogSourceLine?>
    {
        private bool _readFromStream = leaderLines == null;
        private readonly IEnumerator<LogSourceLine>? _leaderLinesEnumerator = leaderLines?.GetEnumerator();
        
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

            var readLine = await logSourceReader.ReadLineAsync(cancellationToken);

            if (readLine == null) return false;

            Current = readLine;
            return true;
        }

        public LogSourceLine? Current { get; private set; } 
        
        public ValueTask DisposeAsync()
        {
            _readFromStream = leaderLines == null;
            return ValueTask.CompletedTask;
        }
    }
}