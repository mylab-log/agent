namespace MyLab.LogAgent
{
    public static class LogPropertyNames
    {
        public const string Exception = "exception";
        public const string Message = "message";
        public const string Level = "level";
        public const string Time = "@timestamp";
        public const string Format = "format";
        public const string Container = "container";
        public const string Category = "category";
        public const string OriginMessage = "origin-message";
        public const string ParsingFailedFlag = "parsing-failed";
        public const string ParsingFailureReason = "parsing-failure-reason";
        public const string UnsupportedFormatFlag = "unsupported-format";
        public const string HostAltName = "host-prop";
        public const string ContainerLabels = "docker-labels";
    }
}
