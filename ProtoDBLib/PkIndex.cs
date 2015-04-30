using ProtoBuf;
using System.Collections.Generic;

namespace PointProtoDB
{
    [ProtoContract(IgnoreListHandling = true)]
    public class PkIndex<K> : IEnumerable<long>
    {
        [ProtoMember(1)]
        public Dictionary<K, long> _index;

        public PkIndex()
        {
            _index = new Dictionary<K, long>();
        }

        public void Add(K key, long data)
        {
            _index.Add(key, data);
        }

        public void Remove(K key)
        {
            _index.Remove(key);
        }

        public long this[K key]
        {
            get { return _index[key]; }
        }

        public IEnumerable<K> GetKeys()
        {
            return _index.Keys;
        }

        public IEnumerator<long> GetEnumerator()
        {
            return _index.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _index.Values.GetEnumerator();
        }
    }
}