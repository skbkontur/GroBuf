using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

using GrEmit;

namespace GroBuf.Readers
{
    internal class ClassReaderBuilder : ReaderBuilderBase
    {
        public ClassReaderBuilder(Type type)
            : base(type)
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("setters_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[])),
                    new KeyValuePair<string, Type>("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), typeof(ulong[])),
                });
            foreach(var member in context.GetDataMembers(Type))
            {
                Type memberType;
                switch(member.Member.MemberType)
                {
                case MemberTypes.Property:
                    memberType = ((PropertyInfo)member.Member).PropertyType;
                    break;
                case MemberTypes.Field:
                    memberType = ((FieldInfo)member.Member).FieldType;
                    break;
                default:
                    throw new NotSupportedException("Data member of type " + member.Member.MemberType + " is not supported");
                }
                context.BuildConstants(memberType);
            }
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            MemberInfo[] dataMembers;
            ulong[] hashCodes;
            BuildMembersTable(context.Context, out hashCodes, out dataMembers);

            var il = context.Il;
            var end = context.Length;
            var typeCode = context.TypeCode;

            var setters = dataMembers.Select(member => member == null ? default(KeyValuePair<Delegate, IntPtr>) : GetMemberSetter(context.Context, member)).ToArray();

            var settersField = context.Context.InitConstField(Type, 0, setters.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, setters.Select(pair => pair.Key).ToArray());
            var hashCodesField = context.Context.InitConstField(Type, 2, hashCodes);

            context.IncreaseIndexBy1(); // index = index + 1
            context.AssertTypeCode(GroBufTypeCode.Object);

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [(uint)data[index] = data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]

            il.Dup(); // stack: [data length, data length]
            il.Stloc(end); // end = data length; stack: [data length]

            if(!Type.IsValueType)
            {
                context.LoadResultByRef(); // stack: [data length, ref result]
                il.Ldind(Type); // stack: [data length, result]
                var notNullLabel = il.DefineLabel("notNull");
                il.Brtrue(notNullLabel); // if(result != null) goto notNull; stack: [data length]
                context.LoadResultByRef(); // stack: [data length, ref result]
                ObjectConstructionHelper.EmitConstructionOfType(Type, il);
                il.Stind(Type); // result = new type(); stack: [data length]
                il.MarkLabel(notNullLabel);
            }

            context.StoreObject(Type);

            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(data length == 0) goto done; stack: []
            il.Ldloc(end); // stack: [data length]

            context.AssertLength(); // stack: []

            il.Ldloc(end); // stack: [data length]
            context.LoadIndex(); // stack: [data length, index]
            il.Add(); // stack: [data length + index]
            il.Stloc(end); // end = data length + index

            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);

            il.Ldc_I4(9);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(long)); // stack: [*(int64*)&data[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8; stack: [*(int64*)&data[index] = hashCode]

            il.Dup(); // stack: [hashCode, hashCode]
            il.Ldc_I8(dataMembers.Length); // stack: [hashCode, hashCode, (int64)hashCodes.Length]
            il.Rem(true); // stack: [hashCode, hashCode % hashCodes.Length]
            il.Conv<int>(); // stack: [hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = il.DeclareLocal(typeof(int));
            il.Stloc(idx); // idx = (int)(hashCode % hashCodes.Length); stack: [hashCode]

            context.LoadField(hashCodesField); // stack: [hashCode, hashCodes]
            il.Ldloc(idx); // stack: [hashCode, hashCodes, idx]
            il.Ldelem(typeof(long)); // stack: [hashCode, hashCodes[idx]]

            var skipDataLabel = il.DefineLabel("skipData");
            il.Bne_Un(skipDataLabel); // if(hashCode != hashCodes[idx]) goto skipData; stack: []

            // Read data
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadResultByRef(); // stack: [pinnedData, ref index, ref result]
            context.LoadContext(); // stack: [pinnedData, ref index, ref result, context]

            context.LoadField(settersField); // stack: [pinnedData, ref index, ref result, context, setters]
            il.Ldloc(idx); // stack: [pinnedData, ref index, ref result, context, setters, idx]
            il.Ldelem(typeof(IntPtr)); // stack: [pinnedData, ref index, ref result, context, setters[idx]]
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), Type.MakeByRefType(), typeof(ReaderContext)};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // setters[idx](pinnedData, ref index, ref result, context); stack: []

            var checkIndexLabel = il.DefineLabel("checkIndex");
            il.Br(checkIndexLabel); // goto checkIndex

            il.MarkLabel(skipDataLabel);
            // Skip data
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(byte)); // stack: [data[index]]
            il.Stloc(typeCode); // typeCode = data[index]; stack: []
            context.IncreaseIndexBy1(); // index = index + 1
            context.CheckTypeCode();
            context.SkipValue();

            il.MarkLabel(checkIndexLabel);

            context.LoadIndex(); // stack: [index]
            il.Ldloc(end); // stack: [index, end]
            il.Blt(cycleStartLabel, true); // if(index < end) goto cycleStart; stack: []

            var onDeserializedMethod = Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(method => method.GetCustomAttribute<OnDeserializedAttribute>() != null);
            if(onDeserializedMethod != null)
            {
                var parameters = onDeserializedMethod.GetParameters();
                if(parameters.Length != 1 || parameters[0].ParameterType != typeof(StreamingContext))
                    throw new InvalidOperationException(string.Format("The method '{0}' marked with 'OnDeserialized' attribute must accept exactly one parameter of type '{1}'", onDeserializedMethod, typeof(StreamingContext).FullName));
                context.LoadResult(Type);
                il.Ldc_I4((int)StreamingContextStates.Other);
                il.Newobj(typeof(StreamingContext).GetConstructor(new[] {typeof(StreamingContextStates)}));
                il.Call(onDeserializedMethod);
            }

            il.MarkLabel(doneLabel);
        }

        protected override bool IsReference { get { return true; } }

        private void BuildMembersTable(ReaderTypeBuilderContext context, out ulong[] hashCodes, out MemberInfo[] dataMembers)
        {
            var members = context.GetDataMembers(Type);
            var hashes = GroBufHelpers.CalcHashesAndCheck(members);
            var n = GroBufHelpers.CalcSize(hashes);
            hashCodes = new ulong[n];
            dataMembers = new MemberInfo[n];
            for(var i = 0; i < members.Length; i++)
            {
                var index = (int)(hashes[i] % n);
                hashCodes[index] = hashes[i];
                dataMembers[index] = members[i].Member;
            }
        }

        private KeyValuePair<Delegate, IntPtr> GetMemberSetter(ReaderTypeBuilderContext context, MemberInfo member)
        {
            var method = new DynamicMethod("Set_" + Type.Name + "_" + member.Name + "_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(IntPtr), typeof(int).MakeByRefType(), Type.MakeByRefType(), typeof(ReaderContext)
                                               }, context.Module, true);
            var writableMember = member.TryGetWritableMemberInfo();
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [data]
                il.Ldarg(1); // stack: [data, ref index]
                switch(writableMember.MemberType)
                {
                case MemberTypes.Field:
                    var field = (FieldInfo)writableMember;
                    var done = false;
                    if(member.GetCustomAttributes(typeof(IgnoreDefaultOnMergeAttribute), false).Length > 0 && field.FieldType.IsValueType)
                    {
                        var equalityOperator = field.FieldType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if(field.FieldType.IsPrimitive || equalityOperator != null)
                        {
                            var fieldValue = il.DeclareLocal(field.FieldType);
                            il.Ldarg(2); // stack: [data, ref index, ref result]
                            if(!Type.IsValueType)
                                il.Ldind(Type); // stack: [data, ref index, result]
                            il.Ldfld(field);
                            il.Stloc(fieldValue);
                            il.Ldloca(fieldValue);
                            il.Ldarg(3); // stack: [data, ref index, ref result.field, context]
                            ReaderMethodBuilderContext.CallReader(il, field.FieldType, context); // reader(data, ref index, ref result.field, context); stack: []

                            var temp = il.DeclareLocal(field.FieldType);
                            il.Ldloca(temp);
                            il.Initobj(field.FieldType);
                            il.Ldloc(temp);
                            il.Ldloc(fieldValue);
                            if(field.FieldType.IsPrimitive)
                                il.Ceq();
                            else
                                il.Call(equalityOperator);
                            var notDefaultLabel = il.DefineLabel("notDefault");
                            il.Brfalse(notDefaultLabel);
                            il.Ret();
                            il.MarkLabel(notDefaultLabel);
                            il.Ldarg(2);
                            if(!Type.IsValueType)
                                il.Ldind(Type); // stack: [data, ref index, result]
                            il.Ldloc(fieldValue);
                            il.Stfld(field);
                            done = true;
                        }
                    }
                    if(!done)
                    {
                        il.Ldarg(2); // stack: [data, ref index, ref result]
                        if(!Type.IsValueType)
                            il.Ldind(Type); // stack: [data, ref index, result]
                        il.Ldflda(field); // stack: [data, ref index, ref result.field]
                        il.Ldarg(3); // stack: [data, ref index, ref result.field, context]
                        ReaderMethodBuilderContext.CallReader(il, field.FieldType, context); // reader(data, ref index, ref result.field, context); stack: []
                    }
                    break;
                case MemberTypes.Property:
                    var property = (PropertyInfo)writableMember;
                    var propertyValue = il.DeclareLocal(property.PropertyType);
                    if(context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
                    {
                        var getter = property.GetGetMethod(true);
                        if(getter == null)
                            throw new MissingMethodException(Type.Name, property.Name + "_get");
                        il.Ldarg(2); // stack: [data, ref index, ref result]
                        if(!Type.IsValueType)
                            il.Ldind(Type); // stack: [data, ref index, result]
                        il.Call(getter, Type); // stack: [ data, ref index, result.property]
                        il.Stloc(propertyValue); // propertyValue = result.property; stack: [data, ref index]
                    }
                    il.Ldloca(propertyValue); // stack: [data, ref index, ref propertyValue]
                    il.Ldarg(3); // stack: [data, ref index, ref propertyValue, context]
                    ReaderMethodBuilderContext.CallReader(il, property.PropertyType, context); // reader(data, ref index, ref propertyValue, context); stack: []
                    if(member.GetCustomAttributes(typeof(IgnoreDefaultOnMergeAttribute), false).Length > 0 && property.PropertyType.IsValueType)
                    {
                        var equalityOperator = property.PropertyType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if(property.PropertyType.IsPrimitive || equalityOperator != null)
                        {
                            var temp = il.DeclareLocal(property.PropertyType);
                            il.Ldloca(temp);
                            il.Initobj(property.PropertyType);
                            il.Ldloc(temp);
                            il.Ldloc(propertyValue);
                            if(property.PropertyType.IsPrimitive)
                                il.Ceq();
                            else
                                il.Call(equalityOperator);
                            var notDefaultLabel = il.DefineLabel("notDefault");
                            il.Brfalse(notDefaultLabel);
                            il.Ret();
                            il.MarkLabel(notDefaultLabel);
                        }
                    }
                    il.Ldarg(2); // stack: [ref result]
                    if(!Type.IsValueType)
                        il.Ldind(Type); // stack: [result]
                    il.Ldloc(propertyValue); // stack: [result, propertyValue]
                    var setter = property.GetSetMethod(true);
                    if(setter == null)
                        throw new MissingMethodException(Type.Name, property.Name + "_set");
                    il.Call(setter, Type); // result.property = propertyValue
                    break;
                default:
                    throw new NotSupportedException("Data member of type '" + member.MemberType + "' is not supported");
                }
                il.Ret();
            }
            var @delegate = method.CreateDelegate(typeof(ReaderDelegate<>).MakeGenericType(Type));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }
    }
}