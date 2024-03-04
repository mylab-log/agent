using System.Text.RegularExpressions;

namespace MyLab.LogAgent.Tools
{
    class LabelFilter
    {
        private readonly LabelFilterListItem[]? _whiteList;
        private readonly LabelFilterListItem[]? _blackList;
        private readonly LabelFilterListItem[]? _serviceList;

        public LabelFilter(
            string[]? whiteList = null,
            string[]? blackList = null,
            string[]? serviceList = null
            )
        {
            _whiteList = whiteList?
                .Select(l => new LabelFilterListItem(l))
                .ToArray();
            _blackList = blackList?
                .Select(l => new LabelFilterListItem(l))
                .ToArray();
            _serviceList = serviceList?
                .Select(l => new LabelFilterListItem(l))
                .ToArray();
        }

        public bool IsMatch (string labelKey)
        {
            bool isServiceLabel = _serviceList != null && IsListMatch(labelKey, _serviceList);

            var whiteList = 
                isServiceLabel && _whiteList != null 
                    ? _whiteList.Where(itm => !itm.AllowForAll)
                    : _whiteList;

            if (whiteList != null && !IsListMatch(labelKey, whiteList))
                return false;

            return _blackList == null || !IsListMatch(labelKey, _blackList);
        }

        private static bool IsListMatch(string test, IEnumerable<LabelFilterListItem> list)
        {
            return list.Any(s => s.IsMatch(test));
        }

        
    }

    class LabelFilterListItem
    {
        public string Value { get; }
        public bool RegexMode { get; }
        public bool AllowForAll { get; }

        public LabelFilterListItem(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (value == "*")
            {
                AllowForAll = true;
                Value = value;
            }
            else if (value.Contains('*'))
            {
                RegexMode = true;
                Value = WildCardToRegular(value);
            }
            else
            {
                RegexMode = false;
                Value = value;
            }
        }

        public bool IsMatch(string test)
        {
            return AllowForAll || (RegexMode? Regex.IsMatch(test, Value) : test == Value);
        }

        private static string WildCardToRegular(string value) => 
            "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }
}
