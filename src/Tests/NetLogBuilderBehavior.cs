﻿using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Model;

namespace Tests
{
    public class NetLogBuilderBehavior
    {
        [Fact]
        public void ShouldBuildSimple()
        {
            //Arrange
            string[] lines = new[]
            {
                "\u001b[41m\u001b[30mfail\u001b[39m\u001b[22m\u001b[49m: MyLab.PrometheusAgent.Services.TargetsMetricProvider[0]\n",
                "      Message: Connection refused (infonot-doc-storage-antivirus-processor:80)\n",
            };

            var b = new NetLogReader();
            var readerResults = new[]
            {
                b.ApplyNexLine(lines[0]),
                b.ApplyNexLine(lines[1])
            };

            //Act
            var str = b.BuildString();

            //Assert
            Assert.True(readerResults.All(rr => rr == LogReaderResult.Accepted));
            Assert.Equal(LogLevel.Error, str.ExtractedLogLevel);
            Assert.Contains("MyLab.PrometheusAgent.Services.TargetsMetricProvider[0]", str.Text);
            Assert.Contains("Message: Connection refused (infonot-doc-storage-antivirus-processor:80)", str.Text);
        }
    }
}
