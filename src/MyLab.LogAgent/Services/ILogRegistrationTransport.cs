using MyLab.LogAgent.Model;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsAdapter.Indexing;

namespace MyLab.LogAgent.Services
{
    public interface ILogRegistrationTransport
    {
        Task RegisterLogsAsync(IEnumerable<LogRecord> logRecords);
    }

    class LogRegistrationTransport : ILogRegistrationTransport
    {
        private readonly IEsIndexer<EsLogRecord> _esIndexer;

        public LogRegistrationTransport(IEsIndexer<EsLogRecord> esIndexer)
        {
            _esIndexer = esIndexer;
        }

        public Task RegisterLogsAsync(IEnumerable<LogRecord> logRecords)
        {
            var recs = logRecords.Select(EsLogRecord.FromLogRecord);

            return _esIndexer.BulkAsync(new EsBulkIndexingRequest<EsLogRecord>
            {
                CreateList = recs.ToArray()
            });
        }
    }

    [EsBindingKey("log")]
    class EsLogRecord : Dictionary<string, string>
    {
        public static EsLogRecord FromLogRecord(LogRecord logRecord)
        {
            var esLogRecord = new EsLogRecord
            {
                { LogPropertyNames.Message, logRecord.Message },
                { LogPropertyNames.Time, logRecord.Time.ToString("O") },
                { LogPropertyNames.Level, logRecord.Level.ToString().ToLower() }
            };

            if (logRecord.Properties != null)
            {
                var pGroups = logRecord.Properties
                    .GroupBy(p => p.Name)
                    .ToDictionary(
                        pg => pg.Key,
                        pg => pg.Select(pgv => pgv.Value).ToArray()
                    );

                foreach (var pGroup in pGroups)
                {
                    esLogRecord.Add(pGroup.Key, pGroup.Value.Length == 1 
                        ? pGroup.Value.First() 
                        : string.Join(", ", pGroup.Value)
                        );
                }
            }

            return esLogRecord;
        }
    }
}
