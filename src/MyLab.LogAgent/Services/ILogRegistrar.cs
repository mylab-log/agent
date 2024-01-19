using Microsoft.Extensions.Options;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using System.Linq;

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
        private readonly ILogRegistrationTransport _registrationTransport;
        private readonly IOptions<LogAgentOptions> _opts;
        private bool _registering;
        private readonly object _buffLock = new ();

        public LogRegistrar(ILogRegistrationTransport registrationTransport, IOptions<LogAgentOptions> opts)
        {
            _registrationTransport = registrationTransport ?? throw new ArgumentNullException(nameof(registrationTransport));
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        public async Task RegisterAsync(LogRecord logRecord)
        {
            ArgumentNullException.ThrowIfNull(logRecord, nameof(logRecord));

            _buff.Add(logRecord);
            await TryRegisterLogsFromBufferAsync();

        }

        public Task FlushAsync()
        {
            return TryRegisterLogsFromBufferAsync(force: true);
        }

        private async Task TryRegisterLogsFromBufferAsync(bool force = false)
        {
            if (!_registering && _buff.Count != 0 && (force || _opts.Value.OutgoingBufferSize <= _buff.Count))
            {
                _registering = true;

                try
                {
                    var buffClone = _buff.ToArray();
                    await _registrationTransport.RegisterLogsAsync(buffClone);

                    lock (_buffLock)
                    {
                        foreach (var cloneRec in buffClone)
                        {
                            _buff.Remove(cloneRec);
                        }
                    }
                }
                finally
                {
                    _registering = false;
                }
            }
        }
    }
}
