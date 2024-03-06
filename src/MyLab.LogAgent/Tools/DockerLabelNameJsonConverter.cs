using System.Linq.Expressions;
using MyLab.LogAgent.Model;
using Newtonsoft.Json;

namespace MyLab.LogAgent.Tools
{
    class DockerLabelNameJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString() ?? "[null]");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DockerLabelName);
        }
    }
}
