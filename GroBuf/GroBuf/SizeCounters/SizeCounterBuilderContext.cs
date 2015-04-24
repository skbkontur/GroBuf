using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GroBuf.DataMembersExtractors;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterBuilderContext
    {
        public SizeCounterBuilderContext(GroBufWriter groBufWriter, ModuleBuilder module, Type constantsType, Dictionary<Type, FieldInfo[]> fields, ISizeCounterCollection sizeCounterCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufWriter = groBufWriter;
            Module = module;
            ConstantsType = constantsType;
            this.fields = fields;
            this.sizeCounterCollection = sizeCounterCollection;
            this.dataMembersExtractor = dataMembersExtractor;
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
            return sizeCounters.Values.Cast<CompiledDynamicMethod>().ToArray();
        }

        public void SetSizeCounterMethod(Type type, DynamicMethod method)
        {
            if(sizeCounters[type] != null)
                throw new InvalidOperationException();
            sizeCounters[type] = new CompiledDynamicMethod {Method = method, Index = sizeCounters.Count};
        }

        public void SetSizeCounterPointer(Type type, IntPtr sizeCounterPointer, Delegate sizeCounter)
        {
            if(sizeCounters[type] == null)
                throw new InvalidOperationException();
            var compiledDynamicMethod = (CompiledDynamicMethod)sizeCounters[type];
            compiledDynamicMethod.Pointer = sizeCounterPointer;
            compiledDynamicMethod.Delegate = sizeCounter;
        }

        public CompiledDynamicMethod GetCounter(Type type, bool ignoreCustomSerialization = false)
        {
            var sizeCounter = (CompiledDynamicMethod)sizeCounters[type];
            if(sizeCounter == null)
            {
                sizeCounterCollection.GetSizeCounterBuilder(type, ignoreCustomSerialization).BuildSizeCounter(this);
                sizeCounter = (CompiledDynamicMethod)sizeCounters[type];
                if(sizeCounter == null)
                    throw new InvalidOperationException();
            }
            return sizeCounter;
        }

        public GroBufWriter GroBufWriter { get; private set; }
        public ModuleBuilder Module { get; set; }
        public Type ConstantsType { get; set; }

        private Action BuildFieldInitializer<T>(FieldInfo field, T value)
        {
            var method = new DynamicMethod(field.Name + "_Init_" + Guid.NewGuid(), typeof(void), new[] {typeof(T)}, Module);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0);
                il.Stfld(field);
                il.Ret();
            }
            var action = (Action<T>)method.CreateDelegate(typeof(Action<T>));
            return () => action(value);
        }

        private readonly Dictionary<Type, FieldInfo[]> fields;
        private readonly ISizeCounterCollection sizeCounterCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly Hashtable sizeCounters = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}