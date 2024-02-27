using MyLab.LogAgent.LogFormats;

namespace Tests
{
    public class MultilineLogReaderBehavior
    {
        [Theory]
        [MemberData(nameof(GetNewRecordDetectionCases))]
        public void ShouldDetectNewRecord(string text, int expectedNewRecordPosition)
        {
            //Arrange
            var lines = text.Split("\n");
            var reader = new MultilineLogReader(true);

            int newRecordPosition = -1;

            //Act
            for (int i = 0; i < lines.Length; i++)
            {
                var readRes = reader.ApplyNexLine(lines[i]);
                if (readRes == LogReaderResult.NewRecordDetected)
                {
                    newRecordPosition = i;
                    break;
                }
            }

            //Assert
            Assert.Equal(expectedNewRecordPosition, newRecordPosition);
        }

        public static object[][] GetNewRecordDetectionCases()
        {
            return new[]
            {
                new object[]
                {
                    """
                    Message1
                        Sub content1
                        Sub content2
                        Sub content3
                        
                    Message2
                        Sub content1
                        Sub content2
                        Sub content3
                    """,
                    5
                },
                new object[]
                {
                    """
                    Message1
                        Sub content1
                    
                        Sub content2
                        Sub content3

                    Message2
                        Sub content1
                        Sub content2
                        Sub content3
                    """,
                    6
                },
                new object[]
                {
                    """
                    Message1-1
                        Sub content1
                        Sub content2
                        Sub content3
                    Message1-2
                        Sub content1
                        
                    Message2
                        Sub content1
                        Sub content2
                        Sub content3
                    """,
                    7
                },
                new object[]
                {
                    """
                    Message1

                    Message1
                    """,
                    2
                },
                new object[]
                {
                    """
                    Message1-1
                    Message1-2
                    """,
                    -1
                }
            };
        }
    }
}
