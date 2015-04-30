using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PointProtoDB
{
    public enum CreateMode : byte
    { 
        Overwrite, Create, OpenOrCreate, Open
    }

    public interface IProtoDB<K, D> : IDisposable where D : class
    {
        void Open(string path, string name, IFormatter serializer, Func<D, K> keyFunction, CreateMode createMode);

        void Close();

        D Read(K key);
        IEnumerable<D> Read(IEnumerable<K> key);

        void Insert(D data);
        void Update(D data);
        void Delete(K key);

        void CreateIndex<I>(string name, Func<D, I> indexFunction, bool unique);
        IEnumerable<D> QueryIndex<I>(string name, I indexKey);

        IEnumerable<K> GetKeys();
    }
}
