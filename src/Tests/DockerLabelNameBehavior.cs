using MyLab.LogAgent.Model;

namespace Tests
{
    public class DockerLabelNameBehavior
    {
        [Fact]
        public void ShouldParseFull()
        {
            //Arrange

            //Act
            var n = DockerLabelName.Parse("foo.bar");

            //Assert
            Assert.Equal("foo", n.Namespace);
            Assert.Equal("bar", n.Local);
            Assert.Equal("foo.bar", n.Full);
            Assert.Equal("foo.bar", n.ToString());
        }

        [Fact]
        public void ShouldCastFull()
        {
            //Arrange

            //Act
            DockerLabelName n = "foo.bar";

            //Assert
            Assert.Equal("foo", n.Namespace);
            Assert.Equal("bar", n.Local);
            Assert.Equal("foo.bar", n.Full);
            Assert.Equal("foo.bar", n.ToString());
        }

        [Fact]
        public void ShouldParseLocal()
        {
            //Arrange

            //Act
            var n = DockerLabelName.Parse("foo_bar");

            //Assert
            Assert.Null(n.Namespace);
            Assert.Equal("foo_bar", n.Local);
            Assert.Equal("foo_bar", n.Full);
            Assert.Equal("foo_bar", n.ToString());
        }

        [Fact]
        public void ShouldCastLocal()
        {
            //Arrange

            //Act
            DockerLabelName n = "foo_bar";

            //Assert
            Assert.Null(n.Namespace);
            Assert.Equal("foo_bar", n.Local);
            Assert.Equal("foo_bar", n.Full);
            Assert.Equal("foo_bar", n.ToString());
        }
    }
}
