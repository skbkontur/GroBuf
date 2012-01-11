using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GroBuf.Tests.TestTools
{
    public class ConcurrentCache<TKey, TValue>
    {
        public ConcurrentCache(Dictionary<TKey, TValue> dictionary, Func<TKey, TValue> extractData)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(dictionary);
            this.extractData = extractData;
        }

        public ConcurrentCache(Func<TKey, TValue> extractData)
            : this(new Dictionary<TKey, TValue>(), extractData)
        {
        }

        public TValue Get(TKey obj)
        {
            return dictionary.GetOrAdd(obj, extractData);
        }

        public bool Contains(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void Remove(TKey key)
        {
            TValue value;
            dictionary.TryRemove(key, out value);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        private readonly Func<TKey, TValue> extractData;
        private readonly ConcurrentDictionary<TKey, TValue> dictionary;
    }
}