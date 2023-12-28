using MyLab.Log;
using YamlDotNet.Serialization;

namespace MyLab.LogAgent.Tools
{
    public static class ExceptionDtoExtensions
    {
        private static readonly ISerializer Serializer = new SerializerBuilder().Build();
        public static string? ToYaml(this ExceptionDto? exceptionDto)
        {
            return exceptionDto != null
                ? Serializer.Serialize(exceptionDto)
                : null;
        }
    }
}
