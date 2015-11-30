using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public static class EnumHelpers
    {
        public static void BuildHashCodesTable(Type type, out int[] values, out ulong[] hashCodes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            var enumValues = fields.Select(field => (int)Enum.Parse(type, field.Name)).ToArray();
            var uniqueValues = new HashSet<int>(enumValues).ToArray();
            var nameHashes = GroBufHelpers.CalcHashesAndCheck(fields.Select(DataMember.Create));
            var hashSet = new HashSet<uint>();
            for(var x = (uint)enumValues.Length;; ++x)
            {
                hashSet.Clear();
                var ok = true;
                foreach(var value in uniqueValues)
                {
                    var item = ((uint)value) % x;
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                hashCodes = new ulong[x];
                values = new int[x];
                for(var i = 0; i < x; ++i)
                    values[i] = -1;
                for(var i = 0; i < enumValues.Length; i++)
                {
                    var value = enumValues[i];
                    var index = ((uint)value) % x;
                    hashCodes[index] = nameHashes[i];
                    values[index] = value;
                }
                break;
            }
        }

        public static void BuildValuesTable(Type type, out int[] values, out ulong[] hashCodes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            var arr = fields.Select(field => (int)Enum.Parse(type, field.Name)).ToArray();
            var hashes = GroBufHelpers.CalcHashesAndCheck(fields.Select(DataMember.Create));
            var hashSet = new HashSet<uint>();
            for(var x = (uint)hashes.Length;; ++x)
            {
                hashSet.Clear();
                var ok = true;
                foreach(var hash in hashes)
                {
                    var item = (uint)(hash % x);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                hashCodes = new ulong[x];
                values = new int[x];
                for(var i = 0; i < hashes.Length; i++)
                {
                    var hash = hashes[i];
                    var index = (int)(hash % x);
                    hashCodes[index] = hash;
                    values[index] = (int)arr.GetValue(i);
                }
                return;
            }
        }
    }
}