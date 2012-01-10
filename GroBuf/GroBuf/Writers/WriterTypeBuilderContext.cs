using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using SKBKontur.GroBuf.DataMembersExtracters;

namespace SKBKontur.GroBuf.Writers
{
    internal class WriterTypeBuilderContext
    {
        public WriterTypeBuilderContext(TypeBuilder typeBuilder, IWriterCollection writerCollection, IDataMembersExtracter dataMembersExtracter)
        {
            TypeBuilder = typeBuilder;
            this.writerCollection = writerCollection;
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

        public void SetWriter(Type type, MethodInfo writer)
        {
            if(writers[type] != null)
                throw new InvalidOperationException();
            writers[type] = writer;
        }

        public MethodInfo GetWriter<T>()
        {
            var type = typeof(T);
            var writer = (MethodInfo)writers[type];
            if(writer == null)
            {
                writer = writerCollection.GetWriter<T>().BuildWriter(this);
                if(writers[type] == null)
                    writers[type] = writer;
                else if((MethodInfo)writers[type] != writer)
                    throw new InvalidOperationException();
            }
            return writer;
        }

        public MethodInfo GetWriter(Type type)
        {
            if(getWriterMethod == null)
                getWriterMethod = ((MethodCallExpression)((Expression<Action<WriterTypeBuilderContext>>)(context => context.GetWriter<int>())).Body).Method.GetGenericMethodDefinition();
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
        private readonly IWriterCollection writerCollection;
        private readonly IDataMembersExtracter dataMembersExtracter;

        private readonly Hashtable writers = new Hashtable();
        private readonly Hashtable fields = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}