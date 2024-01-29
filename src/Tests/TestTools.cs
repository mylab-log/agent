using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLab.LogAgent.Tools.LogMessageProc;

namespace Tests
{
    static class TestTools
    {
        public static ILogMessageExtractor DefaultMessageExtractor = new LogMessageExtractor(500);
    }
}
