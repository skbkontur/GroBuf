using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.Writers
{
    internal class ObjectWriterBuilder : WriterBuilderBase
    {
        public ObjectWriterBuilder()
            : base(typeof(object))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("writers_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[])), // This field is needed only to save references to the dynamic methods. Otherwise GC will destroy them
                });
            Array.ForEach(GroBufHelpers.LeafTypes.Where(type => type != null).ToArray(), type => context.BuildConstants(type));
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            var writers = GroBufHelpers.LeafTypes.Select(type1 => type1 == null ? new KeyValuePair<Delegate, IntPtr>(null, IntPtr.Zero) : GetWriter(context, type1)).ToArray();
            var writersField = context.Context.InitConstField(Type, 0, writers.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, writers.Select(pair => pair.Key).ToArray());

            il.Ldfld(typeof(GroBufHelpers).GetField("LeafTypeHandles", BindingFlags.Public | BindingFlags.Static)); // stack: [LeafTypeHandles]
            context.LoadObj(); // stack: [LeafTypeHandles, obj]
            il.Call(getTypeMethod); // stack: [LeafTypeHandles, obj.GetType()]
            var type = il.DeclareLocal(typeof(Type));
            il.Dup(); // stack: [LeafTypeHandles, obj.GetType(), obj.GetType()]
            il.Stloc(type); // type = obj.GetType(); stack: [LeafTypeHandles, obj.GetType()]
            il.Call(typeTypeHandleProperty.GetGetMethod()); // stack: [LeafTypeHandles, obj.GetType().TypeHandle]
            var typeHandle = il.DeclareLocal(typeof(RuntimeTypeHandle));
            il.Stloc(typeHandle); // typeHandle = obj.GetType().TypeHandle; stack: [LeafTypeHandles]
            il.Ldloca(typeHandle); // stack: [LeafTypeHandles, ref typeHandle]
            il.Call(runtimeTypeHandleValueProperty.GetGetMethod(), typeof(RuntimeTypeHandle)); // stack: [LeafTypeHandles, obj.GetType().TypeHandle.Value]
            var handle = il.DeclareLocal(typeof(IntPtr));
            il.Dup(); // stack: [LeafTypeHandles, obj.GetType().TypeHandle.Value, obj.GetType().TypeHandle.Value]
            il.Stloc(handle); // handle = obj.GetType().TypeHandle.Value; stack: [LeafTypeHandles, handle]
            il.Ldc_I4(writers.Length); // stack: [LeafTypeHandles, handle, LeafTypeHandles.Length]
            il.Rem(true); // stack: [LeafTypeHandles, handle % LeafTypeHandles.Length]
            var index = il.DeclareLocal(typeof(int));
            il.Conv<int>(); // stack: [LeafTypeHandles, (int)(handle % LeafTypeHandles.Length)]
            il.Dup(); // stack: [LeafTypeHandles, (int)(handle % LeafTypeHandles.Length), (int)(handle % LeafTypeHandles.Length)]
            il.Stloc(index); // index = (int)(handle % LeafTypeHandles.Length); stack: [LeafTypeHandles, index]
            il.Ldelem(typeof(IntPtr)); // stack: [LeafTypeHandles[index]]
            il.Ldloc(handle); // stack: [LeafTypeHandles[index], handle]
            var tryAsArrayLabel = il.DefineLabel("tryAsArray");
            il.Bne_Un(tryAsArrayLabel); // if(LeafTypeHandles[index] != handle) goto tryAsArray; stack: []
            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadResult(); // stack: [obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj, writeEmpty, result, ref index, context]
            context.LoadField(writersField); // stack: [obj, writeEmpty, result, ref index, context, writers]
            il.Ldloc(index); // stack: [obj, writeEmpty, result, ref index, context, writers, index]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, result, ref index, context, writers[index]]
            var parameterTypes = new[] {typeof(object), typeof(bool), typeof(byte*), typeof(int).MakeByRefType(), typeof(WriterContext)};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // stack: [writers[index](obj, writeEmpty, result, ref index, context)]
            il.Ret();

            il.MarkLabel(tryAsArrayLabel);
            il.Ldloc(type); // stack: [obj.GetType()]
            il.Call(typeIsArrayProperty.GetGetMethod()); // stack: [obj.GetType().IsArray]
            var writeNullLabel = il.DefineLabel("writeNull");
            il.Brfalse(writeNullLabel);
            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadResult(); // stack: [obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj, writeEmpty, result, ref index, context]
            context.LoadField(writersField); // stack: [obj, writeEmpty, result, ref index, context, writers]
            il.Ldc_I4(Array.IndexOf(GroBufHelpers.LeafTypes, typeof(Array))); // stack: [obj, writeEmpty, result, ref index, context, writers, index of typeof(Array)]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, result, ref index, context, writers[index of typeof(Array)]]
            parameterTypes = new[] {typeof(object), typeof(bool), typeof(byte*), typeof(int).MakeByRefType(), typeof(WriterContext)};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // stack: [writers[index of typeof(Array)](obj, writeEmpty, result, ref index, context)]
            il.Ret();

            il.MarkLabel(writeNullLabel);
            context.WriteNull();
        }

        protected override bool IsReference { get { return false; } }

        private static KeyValuePair<Delegate, IntPtr> GetWriter(WriterMethodBuilderContext context, Type type)
        {
            var method = new DynamicMethod("CastTo_" + type.Name + "_AndWrite_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(object), typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)
                                               }, context.Context.Module, true);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [obj]
                if(type.IsValueType)
                    il.Unbox_Any(type); // stack: [(type)obj]
                else
                    il.Castclass(type); // stack: [(type)obj]
                il.Ldarg(1); // stack: [(type)obj, writeEmpty]
                il.Ldarg(2); // stack: [(type)obj, writeEmpty, result]
                il.Ldarg(3); // stack: [(type)obj, writeEmpty, result, ref index]
                il.Ldarg(4); // stack: [(type)obj, writeEmpty, result, ref index, context]
                context.CallWriter(il, type);
                il.Ret();
            }
            var @delegate = method.CreateDelegate(typeof(WriterDelegate<object>));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly PropertyInfo typeTypeHandleProperty = (PropertyInfo)((MemberExpression)((Expression<Func<Type, RuntimeTypeHandle>>)(type => type.TypeHandle)).Body).Member;
        private static readonly PropertyInfo runtimeTypeHandleValueProperty = (PropertyInfo)((MemberExpression)((Expression<Func<RuntimeTypeHandle, IntPtr>>)(handle => handle.Value)).Body).Member;
        private static readonly PropertyInfo typeIsArrayProperty = (PropertyInfo)((MemberExpression)((Expression<Func<Type, bool>>)(type => type.IsArray)).Body).Member;
    }
}