using Microsoft.Extensions.DependencyInjection;
using MyLab.LogAgent;

namespace IntegrationTests
{
    public class LogAgentBehavior
    {
        [Fact]
        public async Task ShouldIndexMultilineDockerLogs()
        {
            //Arrange
            var srv = new ServiceCollection()
                .AddLogAgentLogic()
                .ConfigureLogAgentLogic(opt =>
                {
                    opt.DockerContainersPath = Path.Combine( Directory.GetCurrentDirectory(), "logs");
                    opt.OutgoingBufferSize = 1;
                });

            //Act


            //Assert

        }
    }
}