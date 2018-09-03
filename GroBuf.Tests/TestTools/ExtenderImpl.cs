using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Tests.TestTools
{
    public class ExtenderImpl
    {
        public ExtenderImpl(Func<Type, IEnumerable<PropertyInfo>> scanProperties)
        {
            this.scanProperties = scanProperties;
        }

        public ExtenderImpl()
            : this(ScanProperties.AllPublic)
        {
        }

        public void Extend(Type type, object source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Action<object> extender = GetExtender(type);
            extender(source);
        }

        private Action<object> GetExtender(Type type)
        {
            var result = (Action<object>)typeExtenders[type];
            if (result == null)
            {
                lock (typeExtendersLock)
                {
                    result = (Action<object>)typeExtenders[type];
                    if (result == null)
                    {
                        result = BuildExtender(type);
                        typeExtenders[type] = result;
                    }
                }
            }
            return result;
        }

        private Action<object> BuildExtender(Type type)
        {
            IEnumerable<PropertyInfo> propertyInfos = scanProperties(type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {GetType(), typeof(object)}, GetType(), true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.PropertyType.IsArray)
                    EmitFillOfArrayProperty(il, type, propertyInfo);
                else if (propertyInfo.PropertyType.IsClass)
                    EmitFillOfClassProperty(il, type, propertyInfo);
            }
            il.Emit(OpCodes.Ret);

            var action =
                (Action<ExtenderImpl, object>)dynamicMethod.CreateDelegate(typeof(Action<ExtenderImpl, object>));
            return o => action(this, o);
        }

        private static readonly MethodInfo extendMethodInfo =
            typeof(ExtenderImpl).GetMethod("Extend",
                                           BindingFlags.Public | BindingFlags.Instance, null,
                                           new[] {typeof(Type), typeof(object)},
                                           null);

        private static readonly MethodInfo getTypeFromHandle =
            typeof(Type).GetMethod("GetTypeFromHandle",
                                   BindingFlags.Public |
                                   BindingFlags.Static, null,
                                   new[] {typeof(RuntimeTypeHandle)},
                                   null);

        private readonly Func<Type, IEnumerable<PropertyInfo>> scanProperties;

        private readonly Hashtable typeExtenders = new Hashtable();
        private readonly object typeExtendersLock = new object();

        #region Nested type: ScanProperties

        public static class ScanProperties
        {
            public static readonly Func<Type, IEnumerable<PropertyInfo>> AllPublic =
                type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        #endregion

        #region Emit

        private static void EmitFillOfArrayProperty(ILGenerator il, Type type, PropertyInfo propertyInfo)
        {
            MethodInfo getter = propertyInfo.GetGetMethod();
            if (getter == null)
                return;

            MethodInfo setter = propertyInfo.GetSetMethod();
            Type elementType = propertyInfo.PropertyType.GetElementType();
            ConstructorInfo constructorInfo = elementType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null, new Type[0], null);

            LocalBuilder array = il.DeclareLocal(propertyInfo.PropertyType);
            LocalBuilder index = il.DeclareLocal(typeof(int));

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Callvirt, getter); // stack: [o.Prop]
            il.Emit(OpCodes.Dup); // stack: [o.Prop, o.Prop]
            il.Emit(OpCodes.Stloc, array); // var array = o.Prop; stack = [array]

            Label arrayNotNull = il.DefineLabel();
            Label allDone = il.DefineLabel();

            if (setter != null)
            {
                il.Emit(OpCodes.Brtrue, arrayNotNull); // if (array != null) goto arrayNotNull

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, type); // stack: [o]
                il.Emit(OpCodes.Ldc_I4_0); // stack: [o, 0]
                il.Emit(OpCodes.Newarr, elementType); // stack: [o, new elementType[0]]
                il.Emit(OpCodes.Callvirt, setter); // o.Prop = new elementType[0]

                il.Emit(OpCodes.Br, allDone); // goto allDone
            }
            else
                il.Emit(OpCodes.Brfalse, allDone); // if (array == null) goto allDone

            il.MarkLabel(arrayNotNull);

            if (elementType.IsClass)
            {
                il.Emit(OpCodes.Ldloc, array);
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, index); // index = array.Length; stack = [array.Length]
                il.Emit(OpCodes.Brfalse, allDone); // if (array.Length == 0) goto allDone

                Label cycleStart = il.DefineLabel();
                Label cycleEnd = il.DefineLabel();

                il.MarkLabel(cycleStart);

                il.Emit(OpCodes.Ldloc, array);
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub); // stack: [array, index-1]
                il.Emit(OpCodes.Dup); // stack: [array, index-1, index-1] 
                il.Emit(OpCodes.Stloc, index); // index--; stack: [array, index]
                il.Emit(OpCodes.Ldelem, elementType); // stack = [array[index]]

                Label itemNotNull = il.DefineLabel();

                if (constructorInfo == null)
                    il.Emit(OpCodes.Brfalse, cycleEnd); // if (item == null) goto cycleEnd
                else
                {
                    il.Emit(OpCodes.Brtrue, itemNotNull); // if (item != null) goto itemNotNull
                    il.Emit(OpCodes.Ldloc, array);
                    il.Emit(OpCodes.Ldloc, index);
                    il.Emit(OpCodes.Newobj, constructorInfo);
                    il.Emit(OpCodes.Stelem, elementType); // array[index] = new elementType()
                }

                il.MarkLabel(itemNotNull);

                il.Emit(OpCodes.Ldarg_0); // stack = [this]
                il.Emit(OpCodes.Ldtoken, elementType);
                il.Emit(OpCodes.Call, getTypeFromHandle); // stack = [this, elementType]
                il.Emit(OpCodes.Ldloc, array);
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldelem, elementType); // stack = [this, elementType, array[index]]
                il.Emit(OpCodes.Call, extendMethodInfo); // this.Extend(elementType, array[index])

                il.MarkLabel(cycleEnd);

                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Brtrue, cycleStart); // if (index != 0) goto cycleStart;
            }
            il.MarkLabel(allDone);
        }

        private static void EmitFillOfClassProperty(ILGenerator il, Type type, PropertyInfo propertyInfo)
        {
            MethodInfo getter = propertyInfo.GetGetMethod();
            if (getter == null)
                return;

            MethodInfo setter = propertyInfo.GetSetMethod();
            ConstructorInfo constructorInfo = propertyInfo.PropertyType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);

            il.Emit(OpCodes.Ldarg_1); // stack: [(object) o]
            il.Emit(OpCodes.Castclass, type); // stack: [(Type) o]
            il.Emit(OpCodes.Callvirt, getter); // stack: [o.Prop]

            Label propDone = il.DefineLabel();
            Label propNotNull = il.DefineLabel();

            if (constructorInfo == null || setter == null || type == propertyInfo.PropertyType)
                il.Emit(OpCodes.Brfalse, propDone); // if (o.Prop == null) goto propDone //stack: []
            else
            {
                il.Emit(OpCodes.Brtrue, propNotNull); // if (o.Prop != null) goto propNotNull
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, type); // stack: [o]
                il.Emit(OpCodes.Newobj, constructorInfo); // stack: [o, new Prop()]
                il.Emit(OpCodes.Callvirt, setter); // o.Prop = new Prop()
            }

            il.MarkLabel(propNotNull);

            il.Emit(OpCodes.Ldarg_0); // stack: [extender]
            il.Emit(OpCodes.Ldtoken, propertyInfo.PropertyType);
            il.Emit(OpCodes.Call, getTypeFromHandle); // stack: [extender, PropertyType]
            il.Emit(OpCodes.Ldarg_1); // stack: [extender, PropertyType, o]
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Callvirt, getter); // stack: [extender, PropertyType, o.Prop]
            il.Emit(OpCodes.Call, extendMethodInfo); // extender.Extend(PropertyType, o.Prop)

            il.MarkLabel(propDone);
        }

        #endregion
    }
}