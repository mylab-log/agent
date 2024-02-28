namespace MyLab.LogAgent.LogSourceReaders
{
    public interface ILogSourceReader
    {
        Task<LogSourceLine?> ReadLineAsync(CancellationToken cancellationToken);
    }
}
