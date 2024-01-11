namespace MyLab.LogAgent.LogSourceReaders
{
    interface ILogSourceReader
    {
        Task<LogSourceLine?> ReadLineAsync(CancellationToken cancellationToken);
    }
}
