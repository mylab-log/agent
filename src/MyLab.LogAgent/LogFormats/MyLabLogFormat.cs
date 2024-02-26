using MyLab.Log;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats
{
    class MyLabLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new MultilineLogReader(true);
        }

        public LogRecord Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            if (MyLabLogReader.NewRecordPredicate(logText))
            {
                return MyLabFormatLogic.Parse(logText, messageExtractor);
            }

            if (NetLogReader.NewRecordPredicate(logText))
            {
                return NetFormatLogic.Parse(logText, messageExtractor);
            }

            var resLogRec = messageExtractor.ExtractAndCreateLogRecord(logText);

            FillIfException(resLogRec);

            return resLogRec;
        }

        private void FillIfException(LogRecord resLogRec)
        {
            if (resLogRec.Message.Contains("Unhandled exception"))
                resLogRec.Level = LogLevel.Error;
        }
    }
}
