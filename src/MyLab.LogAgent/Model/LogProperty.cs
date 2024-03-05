using System.Collections;
using System.Collections.ObjectModel;

namespace MyLab.LogAgent.Model
{
    public class LogProperties : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> _map;

        public LogProperties()
            : this(new Dictionary<string, object>())
        {
            
        }

        public LogProperties(LogProperties properties)
            : this(properties._map)
        {

        }

        public LogProperties(IDictionary<string, object> initial)
        {
            _map = new Dictionary<string, object>(initial ?? throw new ArgumentNullException(nameof(initial)));
        }

        public void Add(string name, object value)
        {
            _map[name] = value;
        }

        public void AddRange(IEnumerable<KeyValuePair<string,object>> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (var kv in items)
            {
                _map[kv.Key] = kv.Value;
            }
        }

        public IReadOnlyDictionary<string, object> ToDictionary() => new ReadOnlyDictionary<string, object>(_map);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
