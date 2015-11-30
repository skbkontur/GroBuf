using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.Readers
{
    internal class ReaderConstantsBuilderContext
    {
        public ReaderConstantsBuilderContext(GroBufReader groBufReader, TypeBuilder constantsBuilder, IReaderCollection readerCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufReader = groBufReader;
            ConstantsBuilder = constantsBuilder;
            this.readerCollection = readerCollection;
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
            if (isRoot || GroBufReader.readMethodsWithCustomSerialization[type] == null)
            {
                if(hashtable[type] == null)
                    readerCollection.GetReaderBuilder(type, ignoreCustomSerialization).BuildConstants(this);
            }
        }

        public Dictionary<Type, string[]> GetFields()
        {
            return hashtable.Cast<DictionaryEntry>().ToDictionary(entry => (Type)entry.Key, entry => ((KeyValuePair<string, Type>[])entry.Value).Select(pair => pair.Key).ToArray());
        }

        public GroBufReader GroBufReader { get; private set; }
        public TypeBuilder ConstantsBuilder { get; private set; }

        private readonly Hashtable hashtable = new Hashtable();

        private readonly IReaderCollection readerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;
    }
}