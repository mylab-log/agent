using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;

namespace MyLab.LogAgent.Services
{
    public interface IDockerContainerProvider
    {
        Task<IEnumerable<DockerContainerInfo>> ProvideContainersAsync(CancellationToken cancellationToken);
    }

    class DockerContainerProvider(IOptions<LogAgentOptions> opts) : IDockerContainerProvider
    {
        private readonly DockerClient _dockerClient = new DockerClientConfiguration
            (
                new Uri(opts.Value.DockerUri)
            ).CreateClient();

        public async Task<IEnumerable<DockerContainerInfo>> ProvideContainersAsync(CancellationToken cancellationToken)
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true
                },
                cancellationToken);

            return containers
                .Select(c => 
                    new DockerContainerInfo
                    {
                        Id = c.ID,
                        Name = c.Names.FirstOrDefault(c.ID).TrimStart('/'),
                        LogFormat = c.Labels
                            .Where(kv => kv.Key == "log_format")
                            .Select(kv => kv.Value.ToLower())
                            .FirstOrDefault(),
                        IgnoreStreamType= c.Labels
                            .Where(kv => kv.Key == "log_ignore_stream")
                            .Select(kv => kv.Value.ToLower() == "true")
                            .FirstOrDefault(),
                        Enabled = !c.Labels.Any(l => l.Key == "log_exclude" && l.Value.ToLower() == "true")
                    }
                );
        }
    }
}
