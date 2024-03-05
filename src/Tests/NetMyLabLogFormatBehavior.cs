using MyLab.LogAgent;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Model;

namespace Tests
{
    public class NetMyLabLogFormatBehavior
    {
        [Fact]
        public void ShouldReadSimple()
        {
            //Arrange
            var lines = new []
            {
                "\u001b[41m\u001b[30mfail\u001b[39m\u001b[22m\u001b[49m: MyLab.PrometheusAgent.Services.TargetsMetricProvider[0]\n",
                "      Message: Connection refused (infonot-doc-storage-antivirus-processor:80)\n",
                "      Time: 2023-09-23T00:22:03.216\n",
                "      Facts:\n",
                "        target: http://ololo\n"
            };
            
            var format = new NetMyLabLogFormat();

            var rdr = format.CreateReader()!;

            foreach (var line in lines)
                rdr.ApplyNexLine(line);

            var str = rdr.BuildString();

            //Act
            var logRec = format.Parse(str.Text, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.Equal("Connection refused (infonot-doc-storage-antivirus-processor:80)", logRec.Message);
            Assert.Equal(new DateTime(2023, 09, 23, 00, 22, 03).AddMilliseconds(216), logRec.Time);
            Assert.NotNull(logRec.Properties);
            Assert.Equal(LogLevel.Error, logRec.Level);
            Assert.Contains(logRec.Properties, p => p is { Key: "target", Value: "http://ololo" });
            Assert.Contains(logRec.Properties, p => p is { Key: LogPropertyNames.Category, Value: "MyLab.PrometheusAgent.Services.TargetsMetricProvider" });
        }
    }
}
