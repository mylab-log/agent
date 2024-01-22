using MyLab.Log;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Tools
{
    class LogReader
    {
        private readonly ILogBuilder _logBuilder;
        private readonly ILogFormat _logFormat;
        private readonly ILogSourceReader _logSourceReader;
        private readonly List<LogSourceLine>? _buff;

        public LogReader(ILogFormat logFormat, ILogSourceReader logSourceReader, List<LogSourceLine>? buff)
        {
            _logFormat = logFormat;
            _logSourceReader = logSourceReader ?? throw new ArgumentNullException(nameof(logSourceReader));
            _buff = buff;
            _logBuilder = logFormat.CreateBuilder() ?? new SingleLineLogBuilder();
        }

        public async Task<LogRecord?> ReadLogAsync(CancellationToken cancellationToken)
        {
            _logBuilder.Cleanup();
        
            var logEnum = new LogReaderEnumerable(_logSourceReader, _buff);

            LogRecord? readyLogRecord;
            DateTime? contextDateTime = null;

            await foreach (var nextLine in logEnum.WithCancellation(cancellationToken))
            {
                if(string.IsNullOrWhiteSpace(nextLine!.Text))
                    continue;

                var applyResult = _logBuilder.ApplyNexLine(nextLine!.Text);
                switch (applyResult)
                {
                    case LogReaderResult.Accepted:
                    {
                        contextDateTime ??= nextLine.Time ?? DateTime.Now;
                    }
                        break;
                    case LogReaderResult.CompleteRecord:
                    {
                        readyLogRecord = GetLogRecord(contextDateTime);
                        _buff?.Clear();
                        return readyLogRecord;
                    }
                    case LogReaderResult.NewRecordDetected:
                    {
                        readyLogRecord = GetLogRecord(contextDateTime);
                        _buff?.Clear();
                        _buff?.Add(nextLine);
                        return readyLogRecord;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            readyLogRecord = GetLogRecord(contextDateTime);

            _buff?.Clear();

            return readyLogRecord;
        }
        

        LogRecord? GetLogRecord(DateTime? contextDateTime)
        {
            var logString = _logBuilder.BuildString();
            if (string.IsNullOrWhiteSpace(logString))
                return null;

            try
            {
                var lr = _logFormat.Parse(logString);
                if (lr != null && lr.Time == default)
                    lr.Time = contextDateTime ?? DateTime.Now;
                return lr;
            }
            catch (Exception e)
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
}
