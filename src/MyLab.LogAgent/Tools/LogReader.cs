using MyLab.Log;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Tools
{
    class LogReader(ILogFormat logFormat, StreamReader streamReader, List<string>? buff)
    {
        private readonly ILogBuilder _logBuilder = logFormat.CreateBuilder() ?? new SingleLineLogBuilder();
        
        public async Task<LogRecord?> ReadLogAsync(CancellationToken cancellationToken)
        {
            _logBuilder.Cleanup();
        
            var logEnum = new LogReaderEnumerable(streamReader, buff);

            bool payloadDetected = false;
            LogRecord? readyLogRecord;

            await foreach (var nextLine in logEnum.WithCancellation(cancellationToken))
            {
                if(!payloadDetected && string.IsNullOrWhiteSpace(nextLine))
                    continue;

                var applyResult = _logBuilder.ApplyNexLine(nextLine);
                switch (applyResult)
                {
                    case LogReaderResult.Accepted:
                        break;
                    case LogReaderResult.CompleteRecord:
                    {
                        readyLogRecord = GetLogRecord();
                        buff?.Clear();
                        return readyLogRecord;
                    }
                    case LogReaderResult.NewRecordDetected:
                    {
                        readyLogRecord = GetLogRecord();
                        buff?.Clear();
                        buff?.Add(nextLine);
                        return readyLogRecord;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            readyLogRecord = GetLogRecord();

            buff?.Clear();

            return readyLogRecord;
        }

        LogRecord? GetLogRecord()
        {
            var logString = _logBuilder.BuildString();
            if (string.IsNullOrWhiteSpace(logString))
                return null;

            try
            {
                return logFormat.Parse(logString);
            }
            catch (Exception e)
            {
                return new LogRecord
                {
                    Time = DateTime.Now,
                    Message = "Log parsing error",
                    Exception = ExceptionDto.Create(e).ToYaml(),
                    Properties = new []
                    {
                        new KeyValuePair<string, string>("log-string", logString)
                    }
                };
            }
        }
    }
}
