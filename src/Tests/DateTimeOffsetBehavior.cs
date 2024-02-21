namespace Tests
{
    public class DateTimeOffsetBehavior
    {
        [Fact]
        public void ShouldDeserialize()
        {
            //Arrange
            

            //Act
            var offset = DateTimeOffset.Parse("2023-12-28T13:38:00.000");

            //Assert
            Assert.Equal(DateTimeKind.Unspecified, offset.DateTime.Kind);
        }
    }
}
