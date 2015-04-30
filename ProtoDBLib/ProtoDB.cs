using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace PointProtoDB
{
    public class ProtoDB<K, D> : IProtoDB<K, D> where D : class
    {
        private string _path;
        private string _name;
        private string _dbPath;
        private string _dbPkIdxPath;
        private IFormatter _serializer;
        private FileStream _dbfile;
        private FileStream _dbpkidxfile;
        private Func<D, K> _keyFunction;
        private long _newPos;
        private PkIndex<K> _identity;
        private Dictionary<string, IndexBase<D>> _indexes = new Dictionary<string, IndexBase<D>>();

        private readonly ProtoBuf.PrefixStyle _prefix = ProtoBuf.PrefixStyle.Fixed32;

        public void Open(string path, string name, IFormatter serializer, Func<D, K> keyFunction, CreateMode create)
        {
            _path = path;
            _name = name;
            _dbPath = Path.Combine(_path, _name + ".db");
            _dbPkIdxPath = Path.Combine(_path, _name + ".idx");
            _keyFunction = keyFunction;
            _serializer = serializer;
            _newPos = 0;

            if (create != CreateMode.Open)
            {
                CreateDB(create);
            }

            InitDB();
        }

        private void CreateDB(CreateMode create)
        {
            if (File.Exists(_dbPath) || File.Exists(_dbPkIdxPath))
            {
                switch (create)
                {
                    case CreateMode.Create:
                        throw new Exception("Cannot create DB, DB already exists at " + _path);
                    case CreateMode.OpenOrCreate:
                        return;
                    case CreateMode.Overwrite:
                        File.Delete(_dbPath);
                        File.Delete(_dbPkIdxPath);
                        break;
                }
            }


            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }

            File.Create(_dbPath, 10000, FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.WriteThrough).Close();
            _dbfile = new FileStream(_dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 10000, false);

            File.Create(_dbPkIdxPath, 10000, FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.WriteThrough).Close();
            _dbpkidxfile = new FileStream(_dbPkIdxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 10000, false);

            _identity = new PkIndex<K>();
            Close();
        }

        private void InitDB()
        {
            _dbfile = new FileStream(_dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 10000, false);
            _dbpkidxfile = new FileStream(_dbPkIdxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 10000, false);
            _identity = ProtoBuf.Serializer.DeserializeWithLengthPrefix<PkIndex<K>>(_dbpkidxfile, _prefix);


            foreach (long l in _identity)
            {
                if (l > _newPos)
                {
                    _newPos = l;
                }

                if (_newPos != 0)
                {
                    int objLen = 0;
                    _dbfile.Position = _newPos;
                    ProtoBuf.Serializer.TryReadLengthPrefix(_dbfile, _prefix, out objLen);
                    _newPos += objLen;
                }
            }
        }

        private D Read(long pos)
        {
            _dbfile.Position = pos;
            return ProtoBuf.Serializer.DeserializeWithLengthPrefix<D>(_dbfile, _prefix);
        }

        public D Read(K key)
        {
            return Read(_identity[key]);
        }

        public IEnumerable<D> Read(IEnumerable<K> keys)
        {
            foreach (K key in keys)
            {
                yield return Read(key);
            }
        }

        public void Insert(D data)
        {
            Write(_keyFunction(data), data);
        }

        private void Write(K key, D data)
        {
            long indexPos = _dbfile.Position;
            _dbfile.Position = _newPos;
            ProtoBuf.Serializer.SerializeWithLengthPrefix<D>(_dbfile, data, _prefix);
            _newPos = _dbfile.Position;
            _identity.Add(key, indexPos);

            foreach (IndexBase<D> ind in _indexes.Values)
            {
                ind.Add(data);
            }
        }

        public void Update(D data)
        {
            K key = _keyFunction(data);
            Delete(key);
            Write(key, data);
        }

        public void Delete(K key)
        {
            D data = Read(key);
            _identity.Remove(key);
            foreach (IndexBase<D> ind in _indexes.Values)
            {
                ind.Remove(data);
            }
        }
        
        public void CreateIndex<I>(string name, Func<D, I> indexFunction, bool unique)
        {
            if (_indexes.ContainsKey(name))
            {
                throw new Exception("Index '" + name + "' already exists");
            }

            if (unique)
            {
                UniqueIndex<I, D> newIndex = new UniqueIndex<I, D>(indexFunction);
                foreach (long pos in _identity)
                {
                    newIndex.Add(Read(pos));
                }
                _indexes.Add(name, newIndex);
            }
        }

        public IEnumerable<D> QueryIndex<I>(string name, I indexKey)
        {
            IndexBase<D> ind = _indexes[name];
            switch (ind._type)
            {
                case IndexType.IndexType:
                    return ((Index<I, D>)ind)[indexKey];
                case IndexType.UniqueIndexType:
                    return new D[] { ((UniqueIndex<I, D>)ind)[indexKey] };
            }
            return null; // can't happen
        }

        public IEnumerable<K> GetKeys()
        {
            return _identity.GetKeys();
        }

        public void Close()
        {
            _dbpkidxfile.Position = 0;
            ProtoBuf.Serializer.SerializeWithLengthPrefix<PkIndex<K>>(_dbpkidxfile, _identity, _prefix);
            Console.WriteLine("Index size " + _dbpkidxfile.Position);
            _dbpkidxfile.Flush();
            _dbpkidxfile.Close();
            _dbpkidxfile.Dispose();
            _dbfile.Flush();
            _dbfile.Close();
            _dbfile.Dispose();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
