using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class TypeReaderBuilder
    {
        public TypeReaderBuilder(ModuleBuilder module, IReaderCollection readerCollection)
        {
            this.module = module;
            this.readerCollection = readerCollection;
        }

        public MethodInfo BuildTypeReader<T>()
        {
            var typeBuilder = module.DefineType(typeof(T).Name + "_GroBufReader_" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public);
            var context = new ReaderTypeBuilderContext(readerCollection, typeBuilder);
            var readMethod = context.GetReader<T>();

            var initializer = BuildInitializer(typeBuilder);

            var type = typeBuilder.CreateType();

            type.GetMethod(initializer.Name).Invoke(null, new object[] {context.GetFieldInitializers()});
            return type.GetMethod(readMethod.Name);
        }

        private static MethodInfo BuildInitializer(TypeBuilder typeBuilder)
        {
            var initializer = typeBuilder.DefineMethod("Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] {typeof(Action[])});
            var il = initializer.GetILGenerator();
            var retLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0); // stack: [initializers]
            il.Emit(OpCodes.Brfalse, retLabel); // if(initializers == null) goto ret;
            il.Emit(OpCodes.Ldarg_0); // stack: [initializers]
            il.Emit(OpCodes.Ldlen); // stack: [initializers.Length]
            il.Emit(OpCodes.Dup); // stack: [initializers.Length, initializers.Length]
            var index = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, index); // index = initializers.Length; stack: [initializers.Length]
            il.Emit(OpCodes.Brfalse, retLabel); // if(initializers.Length == 0) goto ret;
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            il.Emit(OpCodes.Ldarg_0); // stack: [initializers]
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
            return initializer;
        }

        private readonly ModuleBuilder module;
        private readonly IReaderCollection readerCollection;
    }
}