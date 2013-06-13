using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.Writers
{
    internal class WriterConstantsBuilderContext
    {
        public WriterConstantsBuilderContext(GroBufWriter groBufWriter, TypeBuilder constantsBuilder, IWriterCollection writerCollection, IDataMembersExtractor dataMembersExtractor, bool ignoreCustomSerialization)
        {
            GroBufWriter = groBufWriter;
            ConstantsBuilder = constantsBuilder;
            this.writerCollection = writerCollection;
            this.dataMembersExtractor = dataMembersExtractor;
            this.ignoreCustomSerialization = ignoreCustomSerialization;
        }

        public MemberInfo[] GetDataMembers(Type type)
        {
            return dataMembersExtractor.GetMembers(type);
        }

        public void SetFields(Type type, KeyValuePair<string, Type>[] fields)
        {
            hashtable[type] = fields;
            foreach(var field in fields)
                ConstantsBuilder.DefineField(field.Key, field.Value, FieldAttributes.Public | FieldAttributes.Static);
        }

        public void BuildConstants(Type type)
        {
            if(hashtable[type] == null)
                writerCollection.GetWriterBuilder(type, ignoreCustomSerialization).BuildConstants(this);
        }

        public Dictionary<Type, string[]> GetFields()
        {
            return hashtable.Cast<DictionaryEntry>().ToDictionary(entry => (Type)entry.Key, entry => ((KeyValuePair<string, Type>[])entry.Value).Select(pair => pair.Key).ToArray());
        }

        public GroBufWriter GroBufWriter { get; private set; }
        public TypeBuilder ConstantsBuilder { get; private set; }

        private readonly Hashtable hashtable = new Hashtable();

        private readonly IWriterCollection writerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;
        private readonly bool ignoreCustomSerialization;
    }

    internal class WriterTypeBuilderContext
    {
        public WriterTypeBuilderContext(GroBufWriter groBufWriter, ModuleBuilder module, Type constantsType, Dictionary<Type, FieldInfo[]> fields, IWriterCollection writerCollection, IDataMembersExtractor dataMembersExtractor, bool ignoreCustomSerialization)
        {
            GroBufWriter = groBufWriter;
            Module = module;
            ConstantsType = constantsType;
            this.fields = fields;
            this.writerCollection = writerCollection;
            this.dataMembersExtractor = dataMembersExtractor;
            this.ignoreCustomSerialization = ignoreCustomSerialization;
        }

        public MemberInfo[] GetDataMembers(Type type)
        {
            return dataMembersExtractor.GetMembers(type);
        }

        public FieldInfo InitConstField<T>(Type type, int index, T value)
        {
            var field = fields[type][index];
            initializers.Add(field.Name, ((Func<FieldInfo, Action>)(f => BuildFieldInitializer(f, value)))(field));
            return field;
        }

        public Action[] GetFieldInitializers()
        {
            return (from object value in initializers.Values select ((Action)value)).ToArray();
        }

        public CompiledDynamicMethod[] GetMethods()
        {
            return writers.Values.Cast<CompiledDynamicMethod>().ToArray();
        }

        public void SetWriterMethod(Type type, DynamicMethod method)
        {
            if(writers[type] != null)
                throw new InvalidOperationException();
            writers[type] = new CompiledDynamicMethod {Method = method, Index = writers.Count};
        }

        public void SetWriterPointer(Type type, IntPtr writerPointer, Delegate writer)
        {
            if(writers[type] == null)
                throw new InvalidOperationException();
            var compiledDynamicMethod = (CompiledDynamicMethod)writers[type];
            compiledDynamicMethod.Pointer = writerPointer;
            compiledDynamicMethod.Delegate = writer;
        }

        public CompiledDynamicMethod GetWriter(Type type)
        {
            var writer = (CompiledDynamicMethod)writers[type];
            if(writer == null)
            {
                writerCollection.GetWriterBuilder(type, ignoreCustomSerialization).BuildWriter(this);
                writer = (CompiledDynamicMethod)writers[type];
                if(writer == null)
                    throw new InvalidOperationException();
            }
            return writer;
        }

        public GroBufWriter GroBufWriter { get; set; }
        public ModuleBuilder Module { get; set; }
        public Type ConstantsType { get; set; }

        private Action BuildFieldInitializer<T>(FieldInfo field, T value)
        {
            var method = new DynamicMethod(field.Name + "_Init_" + Guid.NewGuid(), typeof(void), new[] {typeof(T)}, Module);
            var il = new GroboIL(method);
            il.Ldarg(0);
            il.Stfld(field);
            il.Ret();
            var action = (Action<T>)method.CreateDelegate(typeof(Action<T>));
            return () => action(value);
        }

        private readonly IWriterCollection writerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;
        private readonly bool ignoreCustomSerialization;
        private readonly Dictionary<Type, FieldInfo[]> fields;

        private readonly Hashtable writers = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}