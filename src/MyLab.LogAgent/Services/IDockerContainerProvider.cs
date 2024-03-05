using System.Collections.ObjectModel;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Tools;

namespace MyLab.LogAgent.Services
{
    public interface IDockerContainerProvider
    {
        Task<IEnumerable<DockerContainerInfo>> ProvideContainersAsync(CancellationToken cancellationToken);
    }

    class DockerContainerProvider(IOptions<LogAgentOptions> opts) : IDockerContainerProvider
    {
        public string FormatLabelName = "net.mylab.log.format";
        public string FormatLabelNameOld = "log_format";
        public string IgnoreStreamLabelName = "net.mylab.log.ignore-stream";
        public string IgnoreStreamLabelNameOld = "log_ignore_stream";
        public string ExcludeLabelName = "net.mylab.log.exclude";
        public string ExcludeLabelNameOld = "log_exclude";

        private readonly DockerClient _dockerClient = new DockerClientConfiguration
            (
                new Uri(opts.Value.Docker.SocketUri)
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
                            .Where(kv => kv.Key == FormatLabelName || kv.Key == FormatLabelNameOld)
                            .Select(kv => kv.Value.ToLower())
                            .FirstOrDefault(),
                        
                        IgnoreStreamType= c.Labels
                            .Any(kv => 
                                (
                                    kv.Key == IgnoreStreamLabelName || 
                                    kv.Key == IgnoreStreamLabelNameOld
                                ) && 
                                kv.Value.ToLower() == "true"),
                        
                        Enabled = !c.Labels
                            .Any(l => 
                                (
                                    l.Key == ExcludeLabelName || 
                                    l.Key == ExcludeLabelNameOld
                                ) && 
                                l.Value.ToLower() == "true"),
                        
                        Labels = new ReadOnlyDictionary<string, string>(c.Labels)
                    }
                );
        }
    }
}
