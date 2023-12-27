using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Services
{
    public interface ILogRegistrationTransport
    {
        Task RegisterLogsAsync(IEnumerable<LogRecord> logRecords);
    }

    class LogRegistrationTransport : ILogRegistrationTransport
    {
        public Task RegisterLogsAsync(IEnumerable<LogRecord> logRecords)
        {
            throw new NotImplementedException();
        }
    }
}
