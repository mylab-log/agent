using System.Collections.Generic;
using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Services
{
    interface IContextPropertiesProvider
    {
        IEnumerable<LogProperty> ProvideProperties();
    }

    class ContextPropertiesProvider : IContextPropertiesProvider
    {
        public IEnumerable<LogProperty> ProvideProperties()
        {
            var version = Environment.GetEnvironmentVariable("LOGEGENT_VER");

            return new[]
            {
                new LogProperty
                {
                    Name = LogPropertyNames.AgentVersion,
                    Value = version ?? "undefined"
                }
            };
        }
    }
}
