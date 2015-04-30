using PointProtoDB;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestRig
{
    class Program
    {

        private static void Log(string msg, params object[] param)
        {
            string output = string.Format(msg, param);
            Console.WriteLine("{0}\t{1}", DateTime.Now.ToString("HH:mm:ss:fff"), output);
        }

        static void Main(string[] args)
        {
            try
            {
                IProtoDB<long, Record> db = new ProtoDB<long, Record>();
                string path = @"c:\logs\protodbtest";
                string name = "test";
                
                Log("Create DB");
                db.Open(path, name, ProtoBuf.Serializer.CreateFormatter<Record>(), (x) => x.ID, CreateMode.OpenOrCreate);
                Log("Start writing");
                IEnumerable<long> keys = db.GetKeys();
                long maxKey = 0; 
                if (keys.Count() > 0)
                {
                    maxKey = keys.Max()+1;
                }

                for (long i = maxKey; i < maxKey+100; i++)
                {
                    db.Insert(new Record() { ID = i, Value = 17 + i, SValue = new string('Q', 5) });
                }
                //db.Delete(1);
                Log("Write done");
                db.Close();
                Log("DB closed");

                db = new ProtoDB<long, Record>();
                Log("Open DB");
                db.Open(path, name, Serializer.CreateFormatter<Record>(), (x) => x.ID, CreateMode.Open);
                Log("Start read");
                Record r = db.Read(1);
                Log("Read done");
                db.Close();
                Log("DB closed");

                Log("Done");
            }
            catch (Exception e)
            {
                Log("Exception :" + e);
            }
            Console.ReadKey();

        }
    }
}
