using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GroBuf.DataMembersExtractors;

namespace GroBuf.Readers
{
    internal class ReaderTypeBuilderContext
    {
        public ReaderTypeBuilderContext(GroBufReader groBufReader, ModuleBuilder module, Type constantsType, Dictionary<Type, FieldInfo[]> fields, IReaderCollection readerCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufReader = groBufReader;
            Module = module;
            ConstantsType = constantsType;
            this.fields = fields;
            this.readerCollection = readerCollection;
            this.dataMembersExtractor = dataMembersExtractor;
            Lengths = typeof(GroBufHelpers).GetField("Lengths", BindingFlags.Static | BindingFlags.Public);
        }

        public IDataMember[] GetDataMembers(Type type)
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
            return readers.Values.Cast<CompiledDynamicMethod>().ToArray();
        }

        public void SetReaderMethod(Type type, DynamicMethod method)
        {
            if(readers[type] != null)
                throw new InvalidOperationException();
            readers[type] = new CompiledDynamicMethod {Method = method, Index = readers.Count};
        }

        public void SetReaderPointer(Type type, IntPtr readerPointer, Delegate reader)
        {
            if(readers[type] == null)
                throw new InvalidOperationException();
            var compiledDynamicMethod = (CompiledDynamicMethod)readers[type];
            compiledDynamicMethod.Pointer = readerPointer;
            compiledDynamicMethod.Delegate = reader;
        }

        public CompiledDynamicMethod GetReader(Type type, bool ignoreCustomSerialization = false)
        {
            var reader = (CompiledDynamicMethod)readers[type];
            if(reader == null)
            {
                readerCollection.GetReaderBuilder(type, ignoreCustomSerialization).BuildReader(this);
                reader = (CompiledDynamicMethod)readers[type];
                if(reader == null)
                    throw new InvalidOperationException();
            }
            return reader;
        }

        public GroBufReader GroBufReader { get; private set; }
        public ModuleBuilder Module { get; private set; }
        public Type ConstantsType { get; private set; }
        public FieldInfo Lengths { get; private set; }

        private Action BuildFieldInitializer<T>(FieldInfo field, T value)
        {
            var method = new DynamicMethod(field.Name + "_Init_" + Guid.NewGuid(), typeof(void), new[] {typeof(T)}, Module);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Stfld(field);
                il.Ret();
            }
            var action = (Action<T>)method.CreateDelegate(typeof(Action<T>));
            return () => action(value);
        }

        private readonly Dictionary<Type, FieldInfo[]> fields;
        private readonly IReaderCollection readerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly Hashtable readers = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}