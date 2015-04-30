using ProtoBuf;
using System;
using System.Collections.Generic;

namespace PointProtoDB
{
    [ProtoContract]
    [ProtoInclude(51, typeof(UniqueIndex<,>))]
    [ProtoInclude(52, typeof(Index<,>))]
    public abstract class IndexBase<D>
    {
        public IndexType _type { get; set; }
        public abstract void Add(D data);
        public abstract void Remove(D data);
    }

    [ProtoContract]
    public enum IndexType : byte
    {
        IndexType = 0,
        UniqueIndexType = 1
   
    }

    [ProtoContract(IgnoreListHandling = true)]
    public class UniqueIndex<K, D> : IndexBase<D>, IEnumerable<D>
    {
        [ProtoMember(1)]
        public Dictionary<K, D> _index;

        private Func<D, K> _keyfunc;

        public UniqueIndex(Func<D, K> keyfunc)
        {
            _keyfunc = keyfunc;
            _type = IndexType.UniqueIndexType;
            _index = new Dictionary<K, D>();
        }

        public override void Add(D data)
        {
            _index.Add(_keyfunc(data), data);
        }

        public void Remove(K key)
        {
            _index.Remove(key);
        }

        public override void Remove(D data)
        {
            _index.Remove(_keyfunc(data));
        }

        public D this[K key]
        {
            get { return _index[key]; }
        }

        public IEnumerator<D> GetEnumerator()
        {
            return _index.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _index.Values.GetEnumerator();
        }
    }

    [ProtoContract(IgnoreListHandling = true)]
    public class Index<K, D> : IndexBase<D> //, IEnumerable<D>
    {
        [ProtoMember(1)]
        public Dictionary<K, List<D>> _index;

        private Func<D, K> _keyfunc;

        public Index(Func<D, K> keyfunc)
        {
            _keyfunc = keyfunc;
            _type = IndexType.IndexType;
            _index = new Dictionary<K, List<D>>();
        }


        public override void Add(D data)
        {
            List<D> l = null;
            K key = _keyfunc(data);
            if (!_index.TryGetValue(key, out l))
            {
                l = new List<D>();
                _index.Add(key, l);
            }
            l.Add(data);
        }

        public void Remove(K key)
        {
            _index.Remove(key);
        }

        public override void Remove(D data)
        {
            List<D> l = null;
            if (_index.TryGetValue(_keyfunc(data), out l))
            {
                l.Remove(data);
            }
        }

        public IEnumerable<D> this[K key]
        {
            get { return _index[key]; }
        }

        public IEnumerable<D> this[D data]
        {
            get { return _index[_keyfunc(data)]; }
        }

        //public IEnumerator<D> GetEnumerator()
        //{
        //    return _index.Values.GetEnumerator();
        //}

        //System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //{
        //    return _index.Values.GetEnumerator();
        //}
    }

}
