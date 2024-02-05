using MyLab.LogAgent.Tools.LogMessageExtraction;

namespace Tests
{
    static class TestTools
    {
        public static ILogMessageExtractor DefaultMessageExtractor = new LogMessageExtractor(500);
    }
}
