using MyLab.Log;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.Tools
{
    class LogReader
    {
        private readonly ILogReader _logReader;
        private readonly ILogFormat _logFormat;
        private readonly ILogMessageExtractor _messageExtractor;
        private readonly ILogSourceReader _logSourceReader;
        private readonly List<LogSourceLine>? _buff;

        public LogReader(
            ILogFormat logFormat, 
            ILogMessageExtractor messageExtractor,
            ILogSourceReader logSourceReader, 
            List<LogSourceLine>? buff)
        {
            _logFormat = logFormat;
            _messageExtractor = messageExtractor;
            _logSourceReader = logSourceReader ?? throw new ArgumentNullException(nameof(logSourceReader));
            _buff = buff;
            _logReader = logFormat.CreateReader() ?? new SingleLineLogReader();
        }

        public async Task<LogRecord?> ReadLogAsync(CancellationToken cancellationToken)
        {
            _logReader.Cleanup();
        
            var logEnum = new LogReaderEnumerable(_logSourceReader, _buff);

            LogRecord? readyLogRecord;
            DateTime? contextDateTime = null;
            bool contextErrorFactor = false;

            var cancellableLogEnum = logEnum.WithCancellation(cancellationToken);
            var logEnumerator = cancellableLogEnum.GetAsyncEnumerator();

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
                        _buff?.Clear();
                        return readyLogRecord;
                    }
                    case LogReaderResult.NewRecordDetected:
                    {
                        readyLogRecord = GetLogRecord(contextDateTime, contextErrorFactor);
                        _buff?.Clear();
                        if(nextLine != null)
                            _buff?.Add(nextLine);
                        return readyLogRecord;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            } while (true);
            
            readyLogRecord = GetLogRecord(contextDateTime, contextErrorFactor);

            _buff?.Clear();

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

        private static LogRecord CreateFailLogRecord(string logString, Exception e)
        {
            return new LogRecord
            {
                Time = DateTime.Now,
                Message = "Log parsing error",
                Properties =
                [
                    new LogProperty
                    {
                        Name = "log-string", 
                        Value = logString
                    },
                    new LogProperty
                    {
                        Name = LogPropertyNames.Exception, 
                        Value = ExceptionDto.Create(e).ToYaml()!
                    }
                ]
            };
        }
    }
}
