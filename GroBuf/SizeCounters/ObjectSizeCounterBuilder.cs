using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ObjectSizeCounterBuilder : SizeCounterBuilderBase
    {
        public ObjectSizeCounterBuilder()
            : base(typeof(object))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("counters_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[])) // This field is needed only to save references to the dynamic methods. Otherwise GC will destroy them
                });
            Array.ForEach(GroBufHelpers.LeafTypes.Where(type => type != null).ToArray(), type => context.BuildConstants(type));
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            var counters = GroBufHelpers.LeafTypes.Select(type1 => type1 == null ? new KeyValuePair<Delegate, IntPtr>(null, IntPtr.Zero) : GetCounter(context, type1)).ToArray();
            var countersField = context.Context.InitConstField(Type, 0, counters.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, counters.Select(pair => pair.Key).ToArray());

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
            il.Ldc_I4(counters.Length); // stack: [LeafTypeHandles, handle, LeafTypeHandles.Length]
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
            context.LoadContext(); // stack: [obj, writeEmpty, context]
            context.LoadField(countersField); // stack: [obj, writeEmpty, context, counters]
            il.Ldloc(index); // stack: [obj, writeEmpty, context, counters, index]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, context, counters[index]]
            var parameterTypes = new[] {typeof(object), typeof(bool), typeof(WriterContext)};
            il.Calli(CallingConventions.Standard, typeof(int), parameterTypes); // stack: [counters[index](obj, writeEmpty)]
            il.Ret();

            il.MarkLabel(tryAsArrayLabel);
            il.Ldloc(type); // stack: [obj.GetType()]
            il.Call(typeIsArrayProperty.GetGetMethod()); // stack: [obj.GetType().IsArray]
            var returnForNullLabel = il.DefineLabel("returnForNull");
            il.Brfalse(returnForNullLabel);
            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadContext(); // stack: [obj, writeEmpty, context]
            context.LoadField(countersField); // stack: [obj, writeEmpty, context, counters]
            il.Ldc_I4(Array.IndexOf(GroBufHelpers.LeafTypes, typeof(object[]))); // stack: [obj, writeEmpty, context, counters, index of typeof(object[])]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, context, counters[index of typeof(object[])]]
            parameterTypes = new[] {typeof(object), typeof(bool), typeof(WriterContext)};
            il.Calli(CallingConventions.Standard, typeof(int), parameterTypes); // stack: [counters[index of typeof(object[])](obj, writeEmpty)]
            il.Ret();

            il.MarkLabel(returnForNullLabel);
            context.ReturnForNull();
        }

        protected override bool IsReference { get { return false; } }

        private static KeyValuePair<Delegate, IntPtr> GetCounter(SizeCounterMethodBuilderContext context, Type type)
        {
            var method = new DynamicMethod("CastTo_" + type.Name + "_AndCount_" + Guid.NewGuid(), typeof(int), new[] {typeof(object), typeof(bool), typeof(WriterContext)}, context.Context.Module, true);
            using (var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [obj]
                if (type.IsValueType)
                    il.Unbox_Any(type); // stack: [(type)obj]
                else
                    il.Castclass(type); // stack: [(type)obj]
                il.Ldarg(1); // stack: [(type)obj, writeEmpty]
                il.Ldarg(2); // stack: [(type)obj, writeEmpty, context]
                context.CallSizeCounter(il, type);
                il.Ret();
            }
            var @delegate = method.CreateDelegate(typeof(SizeCounterDelegate<object>));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly PropertyInfo typeTypeHandleProperty = (PropertyInfo)((MemberExpression)((Expression<Func<Type, RuntimeTypeHandle>>)(type => type.TypeHandle)).Body).Member;
        private static readonly PropertyInfo runtimeTypeHandleValueProperty = (PropertyInfo)((MemberExpression)((Expression<Func<RuntimeTypeHandle, IntPtr>>)(handle => handle.Value)).Body).Member;
        private static readonly PropertyInfo typeIsArrayProperty = (PropertyInfo)((MemberExpression)((Expression<Func<Type, bool>>)(type => type.IsArray)).Body).Member;
    }
}