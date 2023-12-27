using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;

namespace MyLab.LogAgent.Services
{
    public interface ILogRegistrar
    {
        Task RegisterAsync(LogRecord logRecord);

        Task FlushAsync();
    }

    class LogRegistrar(ILogRegistrationTransport registrationTransport, IOptions<LogAgentOptions> opts) : ILogRegistrar
    {
        private readonly List<LogRecord> _buff = new ();
        private readonly object _sync = new ();

        public async Task RegisterAsync(LogRecord logRecord)
        {
            if (logRecord == null) 
                throw new ArgumentNullException(nameof(logRecord));

            Monitor.Enter(_sync);

            try
            {
                _buff.Add(logRecord);
                await TryRegisterLogsFromBufferAsync();
            }
            finally
            {
                Monitor.Exit(_sync);
            }
            
        }

        public Task FlushAsync()
        {
            return TryRegisterLogsFromBufferAsync();
        }

        private async Task TryRegisterLogsFromBufferAsync()
        {
            if (opts.Value.OutgoingBufferSize <= _buff.Count)
            {
                await registrationTransport.RegisterLogsAsync(_buff);
                _buff.Clear();
            }
        }
    }
}
