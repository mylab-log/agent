using MyLab.LogAgent.Model;
using Newtonsoft.Json.Linq;

namespace MyLab.LogAgent.LogFormats
{
    interface ILogFormat
    {
        ILogBuilder? CreateBuilder();

        LogRecord? Deserialize(string logText);
    }
}
