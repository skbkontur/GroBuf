using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterTypeBuilderContext
    {
        public SizeCounterTypeBuilderContext(GroBufWriter groBufWriter, TypeBuilder typeBuilder, ISizeCounterCollection sizeCounterCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufWriter = groBufWriter;
            TypeBuilder = typeBuilder;
            this.sizeCounterCollection = sizeCounterCollection;
            this.dataMembersExtractor = dataMembersExtractor;
        }

        public MemberInfo[] GetDataMembers(Type type)
        {
            return dataMembersExtractor.GetMembers(type);
        }

        public FieldInfo BuildConstField<T>(string name, T value)
        {
            return BuildConstField<T>(name, field => BuildFieldInitializer(field, value));
        }

        public FieldInfo BuildConstField<T>(string name, Func<FieldInfo, Action> fieldInitializer)
        {
            var field = TypeBuilder.DefineField(name, typeof(T), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
            fields.Add(name, field);
            initializers.Add(name, fieldInitializer(field));
            return field;
        }

        public Action[] GetFieldInitializers()
        {
            return (from object value in initializers.Values select ((Action)value)).ToArray();
        }

        public void SetCounter(Type type, MethodInfo counter)
        {
            if(counters[type] != null)
                throw new InvalidOperationException();
            counters[type] = counter;
        }

        public MethodInfo GetCounter(Type type)
        {
            var counter = (MethodInfo)counters[type];
            if(counter == null)
            {
                counter = sizeCounterCollection.GetSizeCounterBuilder(type).BuildSizeCounter(this);
                if(counters[type] == null)
                    counters[type] = counter;
                else if((MethodInfo)counters[type] != counter)
                    throw new InvalidOperationException();
            }
            return counter;
        }

        public GroBufWriter GroBufWriter { get; private set; }
        public TypeBuilder TypeBuilder { get; private set; }

        private Action BuildFieldInitializer<T>(FieldInfo field, T value)
        {
            var method = TypeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] {typeof(T)});
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return () => TypeBuilder.GetMethod(method.Name).Invoke(null, new object[] {value});
        }

        private readonly ISizeCounterCollection sizeCounterCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly Hashtable counters = new Hashtable();
        private readonly Hashtable fields = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}