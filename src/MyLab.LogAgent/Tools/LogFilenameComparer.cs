namespace MyLab.LogAgent.Tools
{
    class LogFilenameComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x.EndsWith(".log")) return -1;
            if (y.EndsWith(".log")) return 1;

            var xIndex = int.Parse(x.Substring(x.LastIndexOf(".", StringComparison.InvariantCulture) + 1));
            var yIndex = int.Parse(y.Substring(y.LastIndexOf(".", StringComparison.InvariantCulture) + 1));

            return xIndex.CompareTo(yIndex);
        }
    }
}
