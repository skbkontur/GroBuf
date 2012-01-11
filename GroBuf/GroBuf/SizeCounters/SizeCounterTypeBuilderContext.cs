using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterTypeBuilderContext
    {
        public SizeCounterTypeBuilderContext(TypeBuilder typeBuilder, ISizeCounterCollection sizeCounterCollection, IDataMembersExtracter dataMembersExtracter)
        {
            TypeBuilder = typeBuilder;
            this.sizeCounterCollection = sizeCounterCollection;
            this.dataMembersExtracter = dataMembersExtracter;
        }

        public MemberInfo[] GetDataMembers(Type type)
        {
            return dataMembersExtracter.GetMembers(type);
        }

        public FieldInfo BuildConstField<T>(string name, T value)
        {
            var field = TypeBuilder.DefineField(name, typeof(T), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
            fields.Add(name, field);
            initializers.Add(name, BuildFieldInitializer(field, value));
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

        public MethodInfo GetCounter<T>()
        {
            var type = typeof(T);
            var counter = (MethodInfo)counters[type];
            if(counter == null)
            {
                counter = sizeCounterCollection.GetSizeCounterBuilder<T>().BuildSizeCounter(this);
                if(counters[type] == null)
                    counters[type] = counter;
                else if((MethodInfo)counters[type] != counter)
                    throw new InvalidOperationException();
            }
            return counter;
        }

        public MethodInfo GetCounter(Type type)
        {
            if(getWriterMethod == null)
                getWriterMethod = ((MethodCallExpression)((Expression<Action<SizeCounterTypeBuilderContext>>)(context => context.GetCounter<int>())).Body).Method.GetGenericMethodDefinition();
            return ((MethodInfo)getWriterMethod.MakeGenericMethod(new[] {type}).Invoke(this, new object[0]));
        }

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

        private MethodInfo getWriterMethod;
        private readonly ISizeCounterCollection sizeCounterCollection;
        private readonly IDataMembersExtracter dataMembersExtracter;

        private readonly Hashtable counters = new Hashtable();
        private readonly Hashtable fields = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}