using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;

namespace GroBuf.Writers
{
    internal class WriterTypeBuilder
    {
        public WriterTypeBuilder(GroBufWriter groBufWriter, ModuleBuilder module, IWriterCollection writerCollection, IDataMembersExtractor dataMembersExtractor)
        {
            GroBufWriter = groBufWriter;
            this.module = module;
            this.writerCollection = writerCollection;
            this.dataMembersExtractor = dataMembersExtractor;
        }

        public IntPtr BuildWriter(Type type, bool ignoreCustomSerialization)
        {
            var constantsBuilder = module.DefineType(type.Name + "_GroBufWriter_" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            constantsBuilder.DefineField("pointers", typeof(IntPtr[]), FieldAttributes.Private | FieldAttributes.Static);
            constantsBuilder.DefineField("delegates", typeof(Delegate[]), FieldAttributes.Private | FieldAttributes.Static);
            var constantsBuilderContext = new WriterConstantsBuilderContext(GroBufWriter, constantsBuilder, writerCollection, dataMembersExtractor, ignoreCustomSerialization);
            constantsBuilderContext.BuildConstants(type);
            var constantsType = constantsBuilder.CreateType();
            var fields = constantsBuilderContext.GetFields().ToDictionary(pair => pair.Key, pair => pair.Value.Select(constantsType.GetField).ToArray());
            var context = new WriterTypeBuilderContext(GroBufWriter, module, constantsType, fields, writerCollection, dataMembersExtractor, ignoreCustomSerialization);
            var writer = context.GetWriter(type);

            var initializer = BuildInitializer(constantsType.GetField("pointers", BindingFlags.Static | BindingFlags.NonPublic), constantsType.GetField("delegates", BindingFlags.Static | BindingFlags.NonPublic));

            var compiledDynamicMethods = context.GetMethods();
            var pointers = new IntPtr[compiledDynamicMethods.Length];
            var delegates = new Delegate[compiledDynamicMethods.Length];
            foreach(var compiledDynamicMethod in compiledDynamicMethods)
            {
                int index = compiledDynamicMethod.Index;
                pointers[index] = compiledDynamicMethod.Pointer;
                delegates[index] = compiledDynamicMethod.Delegate;
            }
            initializer(pointers, delegates, context.GetFieldInitializers());
            return writer.Pointer;
        }

        public GroBufWriter GroBufWriter { get; private set; }

        private Action<IntPtr[], Delegate[], Action[]> BuildInitializer(FieldInfo pointersField, FieldInfo delegatesField)
        {
            var initializer = new DynamicMethod("Init", typeof(void), new[] {typeof(IntPtr[]), typeof(Delegate[]), typeof(Action[])}, module, true);
            var il = initializer.GetILGenerator();
            var retLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, pointersField);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stsfld, delegatesField);
            il.Emit(OpCodes.Ldarg_2); // stack: [initializers]
            il.Emit(OpCodes.Brfalse, retLabel); // if(initializers == null) goto ret;
            il.Emit(OpCodes.Ldarg_2); // stack: [initializers]
            il.Emit(OpCodes.Ldlen); // stack: [initializers.Length]
            il.Emit(OpCodes.Dup); // stack: [initializers.Length, initializers.Length]
            var index = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, index); // index = initializers.Length; stack: [initializers.Length]
            il.Emit(OpCodes.Brfalse, retLabel); // if(initializers.Length == 0) goto ret;
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            il.Emit(OpCodes.Ldarg_2); // stack: [initializers]
            il.Emit(OpCodes.Ldloc, index); // stack: [initializers, index]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [initializers, index, 1]
            il.Emit(OpCodes.Sub); // stack: [initializers, index - 1]
            il.Emit(OpCodes.Dup); // stack: [initializers, index - 1, index - 1]
            il.Emit(OpCodes.Stloc, index); // index = index - 1;  // stack: [initializers, index]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [initializers[index]]
            il.Emit(OpCodes.Call, typeof(Action).GetMethod("Invoke")); // intializers[index]()
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Brtrue, cycleStart);
            il.MarkLabel(retLabel);
            il.Emit(OpCodes.Ret);
            return (Action<IntPtr[], Delegate[], Action[]>)initializer.CreateDelegate(typeof(Action<IntPtr[], Delegate[], Action[]>));
        }

        private readonly ModuleBuilder module;
        private readonly IWriterCollection writerCollection;
        private readonly IDataMembersExtractor dataMembersExtractor;
    }
}