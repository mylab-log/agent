using System.Text;
using MyLab.Log;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.Tools
{
    class LogReader
    {
        private readonly ILogReader _logReader;
        private readonly ILogFormat _logFormat;
        private readonly ILogMessageExtractor _messageExtractor;
        private readonly ILogSourceReader _logSourceReader;
        private readonly DefaultLogFormat _defaultFormat = new();

        public List<LogSourceLine>? Buffer { get; set; }

        public LogReader(
            ILogFormat logFormat, 
            ILogMessageExtractor messageExtractor,
            ILogSourceReader logSourceReader)
        {
            _logFormat = logFormat;
            _messageExtractor = messageExtractor;
            _logSourceReader = logSourceReader ?? throw new ArgumentNullException(nameof(logSourceReader));
            _logReader = logFormat.CreateReader() ?? new SingleLineLogReader();
        }

        public async Task<LogRecord?> ReadLogAsync(CancellationToken cancellationToken)
        {
            _logReader.Cleanup();
        
            var logEnum = new LogReaderEnumerable(_logSourceReader, Buffer);

            LogRecord? readyLogRecord;
            DateTime? contextDateTime = null;
            bool contextErrorFactor = false;

            var cancellableLogEnum = logEnum.WithCancellation(cancellationToken);
            var logEnumerator = cancellableLogEnum.GetAsyncEnumerator();

            int originLinesCount = 0;
            int originBytesCount = 0;

            do
            {
                try
                {
                    if (!await logEnumerator.MoveNextAsync())
                        break;
                }
                catch (SourceLogReadingException e)
                {
                    return CreateFailLogRecord(e.SourceText, e);
                }

                var nextLine = logEnumerator.Current;

                if (nextLine != null)
                {
                    originLinesCount += nextLine.Text.Count(ch => ch == '\n')+1;
                    originBytesCount += Encoding.UTF8.GetByteCount(nextLine.Text);
                }

                var applyResult = nextLine != null 
                    ? _logReader.ApplyNexLine(nextLine.Text)
                    : LogReaderResult.CompleteRecord;

                switch (applyResult)
                {
                    case LogReaderResult.Accepted:
                    {
                        contextDateTime ??= nextLine?.Time ?? DateTime.Now;
                        contextErrorFactor = contextErrorFactor || (nextLine?.IsError ?? false);
                    }
                        break;
                    case LogReaderResult.CompleteRecord:
                    {
                        readyLogRecord = GetLogRecord(contextDateTime, contextErrorFactor);
                        Buffer?.Clear();
                        return readyLogRecord;
                    }
                    case LogReaderResult.NewRecordDetected:
                    {
                        readyLogRecord = GetLogRecord(contextDateTime, contextErrorFactor);
                        Buffer?.Clear();
                        if(nextLine != null)
                            Buffer?.Add(nextLine);
                        return readyLogRecord;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            } while (true);
            
            readyLogRecord = GetLogRecord(contextDateTime, contextErrorFactor);

            if (readyLogRecord != null)
            {
                readyLogRecord.OriginBytesCount = originBytesCount;
                readyLogRecord.OriginLinesCount = originLinesCount;
            }

            Buffer?.Clear();

            return readyLogRecord;
        }
        

        LogRecord? GetLogRecord(DateTime? contextDateTime, bool contextErrorFactor)
        {
            var logString = _logReader.BuildString();
            if (string.IsNullOrWhiteSpace(logString.Text))
                return null;

            try
            {
                var lr = _logFormat.Parse(logString.Text, _messageExtractor);
                if (lr != null)
                {
                    if (lr.Time == default)
                    {
                        lr.Time = contextDateTime ?? DateTime.Now;
                    }

                    if (lr.Level == LogLevel.Undefined)
                    {
                        if (logString.ExtractedLogLevel != LogLevel.Undefined)
                            lr.Level = logString.ExtractedLogLevel;
                        else
                        {
                            lr.Level = contextErrorFactor
                                ? LogLevel.Error
                                : LogLevel.Info;
                        }
                    }
                }

                return lr;
            }
            catch (Exception e)
            {
                return CreateFailLogRecord(logString.Text, e);
            }
        }

        private LogRecord CreateFailLogRecord(string logString, Exception e)
        {
            LogRecord? logRecord;

            try
            {
                logRecord = _defaultFormat.Parse(logString, _messageExtractor);

                if (logRecord == null)
                {
                    throw new InvalidOperationException("Can't parse log string");
                }
            }
            catch (Exception exception)
            {
                throw new AggregateException(new Exception[]
                {
                    new FormatException("Can't create parsing error log record", exception),
                    e
                });
            }

            logRecord.Properties ??= new List<LogProperty>();

            logRecord.Properties.Add(new LogProperty
            {
                Name = LogPropertyNames.ParsingFailedFlag,
                Value = "true"
            });
            logRecord.Properties.Add(new LogProperty
            {
                Name = LogPropertyNames.ParsingFailureReason,
                Value = "exception"
            });

            logRecord.Properties.Add(new LogProperty
            {
                Name = LogPropertyNames.Exception,
                Value = ExceptionDto.Create(e).ToYaml() ?? "[no-error-yaml]"
            });

            logRecord.HasParsingError = true;

            return logRecord;
        }
    }
}
