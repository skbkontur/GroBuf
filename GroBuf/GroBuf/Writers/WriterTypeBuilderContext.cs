using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.Writers
{
    internal class WriterTypeBuilderContext
    {
        public WriterTypeBuilderContext(GroBufWriter groBufWriter, TypeBuilder typeBuilder, IWriterCollection writerCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufWriter = groBufWriter;
            TypeBuilder = typeBuilder;
            this.writerCollection = writerCollection;
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

        public void SetWriter(Type type, MethodInfo writer)
        {
            if(writers[type] != null)
                throw new InvalidOperationException();
            writers[type] = writer;
        }

        public MethodInfo GetWriter(Type type)
        {
            var writer = (MethodInfo)writers[type];
            if(writer == null)
            {
                writer = writerCollection.GetWriterBuilder(type).BuildWriter(this);
                if(writers[type] == null)
                    writers[type] = writer;
                else if((MethodInfo)writers[type] != writer)
                    throw new InvalidOperationException();
            }
            return writer;
        }

        public GroBufWriter GroBufWriter { get; set; }
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

        private readonly IWriterCollection writerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly Hashtable writers = new Hashtable();
        private readonly Hashtable fields = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}