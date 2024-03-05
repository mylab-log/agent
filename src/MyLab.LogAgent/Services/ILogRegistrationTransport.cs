using MyLab.LogAgent.Model;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsAdapter.Indexing;

namespace MyLab.LogAgent.Services
{
    public interface ILogRegistrationTransport
    {
        Task RegisterLogsAsync(IEnumerable<LogRecord> logRecords, CancellationToken cancellationToken = default);
    }

    class ElasticsearchLogRegistrationTransport : ILogRegistrationTransport
    {
        private readonly IEsIndexer<EsLogRecord> _esIndexer;

        public ElasticsearchLogRegistrationTransport(IEsIndexer<EsLogRecord> esIndexer)
        {
            _esIndexer = esIndexer;
        }

        public Task RegisterLogsAsync(IEnumerable<LogRecord> logRecords, CancellationToken cancellationToken)
        {
            var recs = logRecords.Select(EsLogRecord.FromLogRecord);

            return _esIndexer.BulkAsync(new EsBulkIndexingRequest<EsLogRecord>
            {
                CreateList = recs.ToArray()
            }, cancellationToken);
        }
    }

    [EsBindingKey("log")]
    class EsLogRecord : Dictionary<string, object>
    {
        public static EsLogRecord FromLogRecord(LogRecord logRecord)
        {
            var resultProperties = new LogProperties(new Dictionary<string, object>
            {
                { LogPropertyNames.Message, logRecord.Message},
                { LogPropertyNames.Time, logRecord.Time.ToString("O")},
                { LogPropertyNames.Level, logRecord.Level.ToString().ToLower()},
                { LogPropertyNames.Format, logRecord.Format ?? "undefined" },
                { LogPropertyNames.Container, logRecord.Container ?? "undefined"}
            });

            if (logRecord.Properties != null)
            {
                resultProperties.AddRange(logRecord.Properties.ToDictionary());
            }
            
            var esLogRecord = new EsLogRecord(
                new Dictionary<string, object>(
                    resultProperties.ToDictionary()
                        .Select(kv => new KeyValuePair<string, object>(
                                NormKey(kv.Key),
                                kv.Value
                            ))
                    )
                );

            return esLogRecord;
        }

        public EsLogRecord()
        {
            
        }

        public EsLogRecord(IDictionary<string, object>  initial)
            :base(initial)
        {
            
        }

        private static string NormKey(string originKey)
        {
            if (originKey == "host")
                return LogPropertyNames.HostAltName;
            if (originKey.Contains('.'))
                return originKey.Replace('.', '-');
            return originKey;
        }
    }
}
