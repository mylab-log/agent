using MyLab.LogAgent.Tools;

namespace Tests;

public class LabelFilterListItemBehavior
{
    [Theory]
    [MemberData(nameof(GetMatchCases))]
    public void ShouldMatch(string filterItem, string test, bool expectedIsMatch)
    {
        //Arrange
        var item = new LabelFilterListItem(filterItem);

        //Act
        var isMatch = item.IsMatch(test);

        //Assert
        Assert.Equal(expectedIsMatch, isMatch);
    }

    public static object[][] GetMatchCases()
    {
        return new object[][]
        {
            new object[]
            {
                "foo", "foo", true
            },
            new object[]
            {
                "foo", "bar", false
            },
            new object[]
            {
                "f*", "foo", true
            },
            new object[]
            {
                "f*o", "foo", true
            }
            ,
            new object[]
            {
                "f*", "bar", false
            }
        };
    }
}