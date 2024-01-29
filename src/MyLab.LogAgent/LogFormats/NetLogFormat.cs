using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools;

namespace MyLab.LogAgent.LogFormats
{
    class NetLogFormat : ILogFormat
    {
        private readonly int _messageLenLim;

        public NetLogFormat(int messageLenLim)
        {
            _messageLenLim = messageLenLim;
        }

        public ILogReader CreateReader()
        {
            return new NetLogReader();
        }

        public LogRecord Parse(string logText)
        {
            string leftText;
            var props = new List<LogProperty>();

            int firstLineIndex = logText.IndexOf('\n');
            if (firstLineIndex != -1)
            {
                string categoryStr = logText.Remove(firstLineIndex).TrimEnd();

                string? category;
                if (categoryStr[^3] == '[' && char.IsDigit(categoryStr[^2]) && categoryStr[^1] == ']')
                {
                    category = categoryStr.Remove(categoryStr.Length - 3);
                }
                else
                {
                    category = categoryStr;
                }

                props.Add(new LogProperty
                {
                    Name = LogPropertyNames.Category, 
                    Value = category
                });
                leftText = logText.Substring(firstLineIndex).TrimStart();
            }
            else
            {
                leftText = logText;
            }

            var message = LogMessage.Extract(leftText, _messageLenLim);

            if (message.Shorted)
            {
                props.Add(new LogProperty
                {
                    Name = LogPropertyNames.OriginMessage, 
                    Value = message.Full
                });
            }

            return new LogRecord
            {
                Message = message.Short,
                Properties = props
            };
        }
    }
}
