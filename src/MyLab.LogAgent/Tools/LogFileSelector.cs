using System.Text.RegularExpressions;

namespace MyLab.LogAgent.Tools
{
    static class LogFileSelector
    {
        public static LogSelectorPredicate Predicate = (cId, filename) => Regex.IsMatch(filename, cId + """-json\.log(.\d?)*""");
    }

    delegate bool LogSelectorPredicate(string containerId, string filename);
}
