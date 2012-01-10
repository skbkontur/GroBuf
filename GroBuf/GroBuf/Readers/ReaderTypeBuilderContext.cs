using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using SKBKontur.GroBuf.DataMembersExtracters;

namespace SKBKontur.GroBuf.Readers
{
    internal class ReaderTypeBuilderContext
    {
        public ReaderTypeBuilderContext(TypeBuilder typeBuilder, IReaderCollection readerCollection, IDataMembersExtracter dataMembersExtracter)
        {
            TypeBuilder = typeBuilder;
            this.readerCollection = readerCollection;
            this.dataMembersExtracter = dataMembersExtracter;
            Lengths = BuildConstField("lengths", GroBufHelpers.Lengths);
        }

        public MemberInfo[] GetDataMembers(Type type)
        {
            return dataMembersExtracter.GetMembers(type);
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

        public void SetReader(Type type, MethodInfo reader)
        {
            if (readers[type] != null)
                throw new InvalidOperationException();
            readers[type] = reader;
        }

        public MethodInfo GetReader<T>()
        {
            var type = typeof(T);
            var reader = (MethodInfo)readers[type];
            if (reader == null)
            {
                reader = readerCollection.GetReader<T>().BuildReader(this);
                if (readers[type] == null)
                    readers[type] = reader;
                else if ((MethodInfo)readers[type] != reader)
                    throw new InvalidOperationException();
            }
            return reader;
        }

        public TypeBuilder TypeBuilder { get; private set; }
        public FieldInfo Lengths { get; private set; }

        public MethodInfo GetReader(Type type)
        {
            if(getReaderMethod == null)
                getReaderMethod = ((MethodCallExpression)((Expression<Action<ReaderTypeBuilderContext>>)(context => context.GetReader<int>())).Body).Method.GetGenericMethodDefinition();
            return ((MethodInfo)getReaderMethod.MakeGenericMethod(new[] {type}).Invoke(this, new object[0]));
        }

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

        private MethodInfo getReaderMethod;

        private readonly IReaderCollection readerCollection;
        private readonly IDataMembersExtracter dataMembersExtracter;

        private readonly Hashtable readers = new Hashtable();
        private readonly Hashtable fields = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}