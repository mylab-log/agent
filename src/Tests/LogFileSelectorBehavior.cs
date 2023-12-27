using MyLab.LogAgent.Tools;

namespace Tests
{
    public class LogFileSelectorBehavior
    {
        [Theory]
        [InlineData("foo", "foo-json.log")]
        [InlineData("foo", "foo-json.log.1")]
        public void ShouldAcceptLogFiles(string containerId, string filename)
        {
            //Arrange
            

            //Act
            bool accept = LogFileSelector.Predicate(containerId, filename);

            //Assert
            Assert.True(accept);
        }

        [Theory]
        [InlineData("foo", "foo-yaml.log")]
        [InlineData("foo", "bar-json.log")]
        [InlineData("foo", "foo-json.txt")]
        public void ShouldFailWrongFiles(string containerId, string filename)
        {
            //Arrange


            //Act
            bool accept = LogFileSelector.Predicate(containerId, filename);

            //Assert
            Assert.False(accept);
        }
    }
}
