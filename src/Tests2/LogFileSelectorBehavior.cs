using MyLab.LogAgent.Tools;

namespace Tests
{
    public class LogFileSelectorBehavior
    {
        [Theory]
        [NUnit.Framework.]
        public void ShouldAcceptLogFiles(string containerId, string filename)
        {
            //Arrange
            

            //Act
            bool accept = LogFileSelector.Predicate(containerId, filename);

            //Assert
            Assert.True(accept);
        }

        [Theory()]
        public void ShouldFailWrongFiles(string containerId, string filename)
        {
            //Arrange


            //Act
            bool accept = LogFileSelector.Predicate(containerId, filename);

            //Assert
            Assert.True(accept);
        }
    }
}
