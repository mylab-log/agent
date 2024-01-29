using MyLab.LogAgent;
using MyLab.LogAgent.LogFormats;

namespace Tests
{
    public class NetLogFormatBehavior
    {
        [Fact]
        public void ShouldReadSimple()
        {
            //Arrange
            var lines = new []
            {
                "\u001b[41m\u001b[30minfo\u001b[41m\u001b[30m: MyApp.Worker[0]",
                "      Hellow!"
            };
            
            var format = new NetLogFormat();

            var rdr = format.CreateReader()!;

            foreach (var line in lines)
                rdr.ApplyNexLine(line);

            var str = rdr.BuildString();

            //Act
            var logRec = format.Parse(str.Text, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.Equal("Hellow!",logRec.Message);
            Assert.NotNull(logRec.Properties);
            Assert.Contains(logRec.Properties, p => p is { Name: LogPropertyNames.Category, Value: "MyApp.Worker" });
        }
    }
}
