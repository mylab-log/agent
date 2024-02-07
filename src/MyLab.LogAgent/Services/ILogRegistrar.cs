using Microsoft.Extensions.Options;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using System.Linq;

namespace MyLab.LogAgent.Services
{
    public interface ILogRegistrar
    {
        Task RegisterAsync(LogRecord logRecord, CancellationToken cancellationToken);

        Task FlushAsync(CancellationToken cancellationToken);
    }

    class LogRegistrar : ILogRegistrar
    {
        private readonly Queue<List<LogRecord>> _buffQ = new ();
        private readonly ILogRegistrationTransport _registrationTransport;
        private readonly IMetricsOperator? _metricsOperator;
        private readonly int _buffSize;

        public LogRegistrar(
            ILogRegistrationTransport registrationTransport, 
            IOptions<LogAgentOptions> opts,
            IMetricsOperator? metricsOperator = null)
        {
            _registrationTransport = registrationTransport ?? throw new ArgumentNullException(nameof(registrationTransport));
            _metricsOperator = metricsOperator;
            _buffSize = opts.Value.OutgoingBufferSize;
        }

        public Task RegisterAsync(LogRecord logRecord, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(logRecord, nameof(logRecord));

            return AddRecordAsync(logRecord, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return UploadRecordsAsync(force:true, cancellationToken);
        }

        Task AddRecordAsync(LogRecord rec, CancellationToken cancellationToken)
        {
            List<LogRecord> buff;

            if (_buffQ.Count != 0)
                buff = _buffQ.Last();
            else
            {
                buff = [];
                _buffQ.Enqueue(buff);
            }

            buff.Add(rec);

            if (buff.Count >= _buffSize)
            {
                _buffQ.Enqueue([]);
            }

            return UploadRecordsAsync(force: false, cancellationToken);
        }

        async Task UploadRecordsAsync(bool force, CancellationToken cancellationToken)
        {
            if (_buffQ.Count == 0 || (_buffQ.Count == 1 && !force)) return;

            var buffToUpload = _buffQ.First();

            DateTime start = DateTime.Now;

            try
            {
                await _registrationTransport.RegisterLogsAsync(buffToUpload, cancellationToken);

                _metricsOperator?.RegisterEsRequest(false, buffToUpload.Count, DateTime.Now - start);
            }
            catch
            {
                _metricsOperator?.RegisterEsRequest(true, buffToUpload.Count, DateTime.Now - start);
                throw;
            }
            
            _buffQ.Dequeue();
        }
    }
}
