using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.Readers
{
    internal class ReaderTypeBuilderContext
    {
        public ReaderTypeBuilderContext(GroBufReader groBufReader, TypeBuilder typeBuilder, IReaderCollection readerCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufReader = groBufReader;
            TypeBuilder = typeBuilder;
            this.readerCollection = readerCollection;
            this.dataMembersExtractor = dataMembersExtractor;
            Lengths = BuildConstField("lengths", GroBufHelpers.Lengths);
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

        public void SetReader(Type type, MethodInfo reader)
        {
            if(readers[type] != null)
                throw new InvalidOperationException();
            readers[type] = reader;
        }

        public MethodInfo GetReader(Type type)
        {
            var reader = (MethodInfo)readers[type];
            if(reader == null)
            {
                reader = readerCollection.GetReaderBuilder(type).BuildReader(this);
                if(readers[type] == null)
                    readers[type] = reader;
                else if((MethodInfo)readers[type] != reader)
                    throw new InvalidOperationException();
            }
            return reader;
        }

        public GroBufReader GroBufReader { get; private set; }
        public TypeBuilder TypeBuilder { get; private set; }
        public FieldInfo Lengths { get; private set; }

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

        private readonly IReaderCollection readerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly Hashtable readers = new Hashtable();
        private readonly Hashtable fields = new Hashtable();
        private readonly Hashtable initializers = new Hashtable();
    }
}