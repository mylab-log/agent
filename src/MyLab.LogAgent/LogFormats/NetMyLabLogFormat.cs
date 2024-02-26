﻿using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats
{
    class NetMyLabLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new NetLogReader();
        }

        public LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            NetFormatLogic.ExtractAndRemoveLevel(logText, out var level, out var logTextWithoutLevel);
            NetFormatLogic.ExtractCategory(logTextWithoutLevel, out var category, out var leftText);

            var catProp = new LogProperty
            {
                Name = LogPropertyNames.Category,
                Value = category
            };

            var rec = MyLabFormatLogic.Parse(leftText, messageExtractor, Enumerable.Repeat(catProp, 1));

            if(level != LogLevel.Undefined)
                rec.Level = level;

            return rec;
        }
    }
}
