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

    class LogRegistrar : ILogRegistrar
    {
        private readonly List<LogRecord> _buff = new ();
        private readonly object _sync = new ();
        private readonly ILogRegistrationTransport _registrationTransport;
        private readonly IOptions<LogAgentOptions> _opts;

        public LogRegistrar(ILogRegistrationTransport registrationTransport, IOptions<LogAgentOptions> opts)
        {
            _registrationTransport = registrationTransport ?? throw new ArgumentNullException(nameof(registrationTransport));
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        public async Task RegisterAsync(LogRecord logRecord)
        {
            ArgumentNullException.ThrowIfNull(logRecord, nameof(logRecord));
            
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
            return TryRegisterLogsFromBufferAsync(force: true);
        }

        private async Task TryRegisterLogsFromBufferAsync(bool force = false)
        {
            if (force || _opts.Value.OutgoingBufferSize <= _buff.Count)
            {
                await _registrationTransport.RegisterLogsAsync(_buff);
                _buff.Clear();
            }
        }
    }
}
