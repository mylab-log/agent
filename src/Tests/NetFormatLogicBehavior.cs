using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLab.LogAgent.LogFormats;

namespace Tests
{
    public class NetFormatLogicBehavior
    {
        [Theory]
        [MemberData(nameof(GetCategoryCases))]
        public void ShouldExtractCategory(string log, string? expectedCategory, string? expectedLeft)
        {
            //Arrange
            

            //Act
            NetFormatLogic.ExtractCategory(log, out var actualCategory, out var actualLeft);

            //Assert
            Assert.Equal(expectedCategory, actualCategory);
            Assert.Equal(expectedLeft, actualLeft);
        }

        public static object[][] GetCategoryCases()
        {
            return new object[][]
            {
                new object[]
                {
                    """
                    FooCategory[0]
                        FooMessage
                    """,
                    "FooCategory",
                    "    FooMessage"
                },
                new object[]
                {
                    """
                    FooCategory
                        AnotherText
                    """,
                    "FooCategory",
                    "    AnotherText"
                }
            };
        }
    }
}
