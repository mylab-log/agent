using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats
{
    partial class NginxLogFormat : ILogFormat
    {
        public const string RemoteAddressProp = "remote-addr";
        public const string RequestProp = "request";
        public const string StatusProp = "status";
        
        public const string NotFound = "[not found]";

        public ILogReader CreateReader()
        {
            return new SingleLineLogReader();
        }

        public LogRecord Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            var props = new List<LogProperty>();
            string message;
            var detectedLogLevel = LogLevel.Undefined;

            if (IsAccessLog(logText))
                AssessLogExtractor.Extract(logText, props, out message);
            else
                ErrorLogExtractor.Extract(logText, props, out message, out detectedLogLevel);

            var resMessage = messageExtractor.ExtractAndCreateLogRecord(message, props);
            resMessage.Level = detectedLogLevel;

            return resMessage;
        }

        private bool IsAccessLog(string logText)
        {
            int firstSpaceIndex = logText.IndexOf(' ');
            if (firstSpaceIndex == -1) return false;

            var firstProperty = logText.Remove(firstSpaceIndex);

            return firstProperty.Count(ch => ch == '.') == 3 && firstProperty.All(ch => ch == '.' || char.IsDigit(ch));
        }

        static string TryCleanupRequest(string originRequest)
        {
            if (originRequest[^8] == 'H' &&
                originRequest[^7] == 'T' &&
                originRequest[^6] == 'T' &&
                originRequest[^5] == 'P' &&
                originRequest[^4] == '/' &&
                char.IsDigit(originRequest[^3]) &&
                originRequest[^2] == '.' &&
                char.IsDigit(originRequest[^1]))
            {
                return originRequest.Remove(originRequest.Length - 9);
            }

            return originRequest;
        }
    }
}
