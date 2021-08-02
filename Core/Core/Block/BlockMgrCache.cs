using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace ETModel
{

    public class BlockMgrCache
    {
        Dictionary<string, Block> cacheDict = new Dictionary<string, Block>();
        List<string> cacheList = new List<string>();

        int cacheCount;
        public BlockMgrCache(int _cacheCount=1000)
        {
            cacheCount = _cacheCount;
        }

        public bool Get(string hash,out Block currValue)
        {
            lock (this)
            {
                return cacheDict.TryGetValue(hash, out currValue);
            }
        }
        public void Add(Block currValue)
        {
            if(currValue!=null)
            {
                lock (this)
                {
                    if (cacheDict.ContainsKey(currValue.hash))
                        return;
                    cacheDict.Add(currValue.hash, currValue);
                    cacheList.Insert(cacheList.Count, currValue.hash);

                    if (cacheList.Count > cacheCount)
                    {
                        cacheDict.Remove(cacheList[0]);
                        cacheList.RemoveAt(0);
                    }
                }
            }
        }

        public void Remove(string hash)
        {
            lock (this)
            {
                cacheDict.Remove(hash);
                cacheList.Remove(hash);
            }
        }

    }

}