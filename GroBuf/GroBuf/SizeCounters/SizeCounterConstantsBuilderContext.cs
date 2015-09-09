using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterConstantsBuilderContext
    {
        public SizeCounterConstantsBuilderContext(GroBufWriter groBufWriter, TypeBuilder constantsBuilder, ISizeCounterCollection sizeCounterCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufWriter = groBufWriter;
            ConstantsBuilder = constantsBuilder;
            this.sizeCounterCollection = sizeCounterCollection;
            this.dataMembersExtractor = dataMembersExtractor;
        }

        public IDataMember[] GetDataMembers(Type type)
        {
            return dataMembersExtractor.GetMembers(type);
        }

        public void SetFields(Type type, KeyValuePair<string, Type>[] fields)
        {
            hashtable[type] = fields;
            foreach(var field in fields)
                ConstantsBuilder.DefineField(field.Key, field.Value, FieldAttributes.Public | FieldAttributes.Static);
        }

        public void BuildConstants(Type type, bool isRoot = false, bool ignoreCustomSerialization = false)
        {
            if(isRoot || GroBufWriter.countersWithCustomSerialization[type] == null)
            {
                if(hashtable[type] == null)
                    sizeCounterCollection.GetSizeCounterBuilder(type, ignoreCustomSerialization).BuildConstants(this);
            }
        }

        public Dictionary<Type, string[]> GetFields()
        {
            return hashtable.Cast<DictionaryEntry>().ToDictionary(entry => (Type)entry.Key, entry => ((KeyValuePair<string, Type>[])entry.Value).Select(pair => pair.Key).ToArray());
        }

        public GroBufWriter GroBufWriter { get; private set; }
        public TypeBuilder ConstantsBuilder { get; private set; }

        private readonly Hashtable hashtable = new Hashtable();

        private readonly ISizeCounterCollection sizeCounterCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;
    }
}