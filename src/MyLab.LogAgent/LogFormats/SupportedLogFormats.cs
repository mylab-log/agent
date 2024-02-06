using System.Collections.ObjectModel;

namespace MyLab.LogAgent.LogFormats
{
    class SupportedLogFormats : ReadOnlyDictionary<string, ILogFormat>
    {
        public static readonly ILogFormat Default = new DefaultLogFormat();
        public static readonly SupportedLogFormats Instance = new ();

        SupportedLogFormats() 
            : base(new Dictionary<string, ILogFormat>
            {
                { "default", Default },
                { "mylab", new MyLabLogFormat() },
                { "mylab-yaml", new MyLabLogFormat() },
                { "net", new NetLogFormat() },
                { "net+mylab", new NetMyLabLogFormat() },
                { "nginx", new NginxLogFormat() }
            })
        {
        }
    }
}
