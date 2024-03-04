using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLab.LogAgent.Tools;

namespace Tests
{
    public class LabelFilterBehavior
    {
        [Theory]
        [InlineData("*", "some-thing", "foo", true)]
        [InlineData("*", "foo", "foo", false)]
        [InlineData("*", "*", "foo", false)]
        [InlineData("foo", "*", "foo", false)]
        [InlineData("foo", "some-thing", "foo", true)]
        [InlineData("f*", "some-thing", "foo", true)]
        [InlineData("f*o", "some-thing", "foo", true)]
        public void ShouldFilter(string wlItm, string blItem, string label, bool expectedMatch)
        {
            //Arrange
            string[]? whiteList = new[]
            {
                wlItm
            };
            string[]? blackList = new[]
            {
                blItem
            };

            var filter = new LabelFilter(whiteList, blackList);
            
            //Act
            var isMatch = filter.IsMatch(label);

            //Assert
            Assert.Equal(expectedMatch, isMatch);
        }

        [Fact]
        public void ShouldNotPassServiceLabelWhenOnlyFullWildcard()
        {
            //Arrange
            string[]? serviceList = new[]
            {
                "foo.*"
            };
            string[]? whiteList = new[]
            {
                "*"
            };

            var filter = new LabelFilter(whiteList, serviceList:serviceList);

            //Act
            var isMatch = filter.IsMatch("foo.bar");

            //Assert
            Assert.False(isMatch);
        }

        [Fact]
        public void ShouldPassServiceLabelWhenHasSpecialWildcard()
        {
            //Arrange
            string[]? serviceList = new[]
            {
                "foo.*"
            };
            string[]? whiteList = new[]
            {
                "*",
                "foo.b*"
            };

            var filter = new LabelFilter(whiteList, serviceList: serviceList);

            //Act
            var isMatch = filter.IsMatch("foo.bar");

            //Assert
            Assert.True(isMatch);
        }
    }
}
