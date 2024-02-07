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
    class EsLogRecord : Dictionary<string, string>
    {
        public static EsLogRecord FromLogRecord(LogRecord logRecord)
        {
            var resultProperties = new List<LogProperty>
            {
                new() {Name = LogPropertyNames.Message, Value = logRecord.Message},
                new() {Name = LogPropertyNames.Time, Value = logRecord.Time.ToString("O")},
                new() {Name = LogPropertyNames.Level, Value = logRecord.Level.ToString().ToLower()},
                new() {Name = LogPropertyNames.Format, Value = logRecord.Format ?? "undefined" },
                new() {Name = LogPropertyNames.Container, Value = logRecord.Container ?? "undefined"}
            };

            if (logRecord.Properties != null)
            {
                resultProperties.AddRange(logRecord.Properties);
            }

            var pGroups = resultProperties
                .GroupBy(p => p.Name)
                .ToDictionary(
                    pg => NormKey(pg.Key),
                    pg => pg.Select(pgv => pgv.Value).ToArray()
                );

            var esLogRecord = new EsLogRecord();

            foreach (var pGroup in pGroups)
            {
                esLogRecord.Add(pGroup.Key, pGroup.Value.Length == 1 
                    ? pGroup.Value.First() 
                    : string.Join(", ", pGroup.Value)
                    );
            }

            return esLogRecord;
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
