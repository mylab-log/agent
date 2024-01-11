namespace MyLab.LogAgent.LogSourceReaders
{
    class AsIsLogSourceReader : ILogSourceReader
    {
        private readonly StreamReader _streamReader;

        public AsIsLogSourceReader(StreamReader streamReader)
        {
            _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        }

        public async Task<LogSourceLine?> ReadLineAsync(CancellationToken cancellationToken)
        {
            if (_streamReader.EndOfStream) return null;

            var line = await _streamReader.ReadLineAsync(cancellationToken);
            return new LogSourceLine(line ?? string.Empty);
        }
    }
}
