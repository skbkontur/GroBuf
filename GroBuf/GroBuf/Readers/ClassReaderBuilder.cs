using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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
                switch(member.Item2.MemberType)
                {
                case MemberTypes.Property:
                    memberType = ((PropertyInfo)member.Item2).PropertyType;
                    break;
                case MemberTypes.Field:
                    memberType = ((FieldInfo)member.Item2).FieldType;
                    break;
                default:
                    throw new NotSupportedException("Data member of type " + member.Item2.MemberType + " is not supported");
                }
                context.BuildConstants(memberType);
            }
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            MemberInfo[] dataMembers;
            ulong[] hashCodes;
            BuildMembersTable(context.Context, out hashCodes, out dataMembers);

            GroboIL il = context.Il;
            var end = context.Length;
            var typeCode = context.TypeCode;

            KeyValuePair<Delegate, IntPtr>[] setters = dataMembers.Select(member => member == null ? default(KeyValuePair<Delegate, IntPtr>) : GetMemberSetter(context.Context, member)).ToArray();

            FieldInfo settersField = context.Context.InitConstField(Type, 0, setters.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, setters.Select(pair => pair.Key).ToArray());
            FieldInfo hashCodesField = context.Context.InitConstField(Type, 2, hashCodes);

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
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldind(typeof(object)); // stack: [result]
                var notNullLabel = il.DefineLabel("notNull");
                il.Brtrue(notNullLabel); // if(result != null) goto notNull; stack: []
                context.LoadResultByRef(); // stack: [ref result]
                ObjectConstructionHelper.EmitConstructionOfType(Type, il);
                il.Stind(typeof(object)); // result = new type(); stack: []
                il.MarkLabel(notNullLabel);
            }

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
            il.Rem(typeof(ulong)); // stack: [hashCode, hashCode % hashCodes.Length]
            il.Conv_I4(); // stack: [hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = il.DeclareLocal(typeof(int));
            il.Stloc(idx); // idx = (int)(hashCode % hashCodes.Length); stack: [hashCode]

            context.LoadField(hashCodesField); // stack: [hashCode, hashCodes]
            il.Ldloc(idx); // stack: [hashCode, hashCodes, idx]
            il.Ldelem(typeof(long)); // stack: [hashCode, hashCodes[idx]]

            var skipDataLabel = il.DefineLabel("skipData");
            il.Bne(skipDataLabel); // if(hashCode != hashCodes[idx]) goto skipData; stack: []

            // Read data
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            context.LoadResultByRef(); // stack: [pinnedData, ref index, dataLength, ref result]

            context.LoadField(settersField); // stack: [pinnedData, ref index, dataLength, ref result, setters]
            il.Ldloc(idx); // stack: [pinnedData, ref index, dataLength, ref result, setters, idx]
            il.Ldelem(typeof(IntPtr)); // stack: [pinnedData, ref index, dataLength, ref result, setters[idx]]
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), typeof(int), Type.MakeByRefType()};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // setters[idx](pinnedData, ref index, dataLength, ref result); stack: []

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
            il.Blt(typeof(uint), cycleStartLabel); // if(index < end) goto cycleStart; stack: []

            il.MarkLabel(doneLabel);
        }

        private void BuildMembersTable(ReaderTypeBuilderContext context, out ulong[] hashCodes, out MemberInfo[] dataMembers)
        {
            var members = context.GetDataMembers(Type);
            ulong[] hashes = GroBufHelpers.CalcHashAndCheck(members.Select(member => member.Item1));
            var hashSet = new HashSet<uint>();
            for(var x = Math.Max((uint)members.Length, 1);; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var hash in hashes)
                {
                    var item = (uint)(hash % x);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                hashCodes = new ulong[x];
                dataMembers = new MemberInfo[x];
                for(int i = 0; i < members.Length; i++)
                {
                    var index = (int)(hashes[i] % x);
                    hashCodes[index] = hashes[i];
                    dataMembers[index] = members[i].Item2;
                }
                return;
            }
        }

        private KeyValuePair<Delegate, IntPtr> GetMemberSetter(ReaderTypeBuilderContext context, MemberInfo member)
        {
            var method = new DynamicMethod("Set_" + Type.Name + "_" + member.Name + "_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), Type.MakeByRefType()
                                               }, context.Module, true);
            var il = new GroboIL(method);

            ReaderMethodBuilderContext.LoadReader(il, member.GetMemberType(), context);

            il.Ldarg(0); // stack: [data]
            il.Ldarg(1); // stack: [data, ref index]
            il.Ldarg(2); // stack: [data, ref index, dataLength]
            switch(member.MemberType)
            {
            case MemberTypes.Field:
                il.Ldarg(3); // stack: [data, ref index, dataLength, ref result]
                if(!Type.IsValueType)
                    il.Ldind(typeof(object)); // stack: [data, ref index, dataLength, result]
                var field = (FieldInfo)member;
                il.Ldflda(field); // stack: [data, ref index, dataLength, ref result.field]
                ReaderMethodBuilderContext.CallReader(il, field.FieldType, context); // reader(data, ref index, dataLength, ref result.field); stack: []
                break;
            case MemberTypes.Property:
                var property = (PropertyInfo)member;
                var propertyValue = il.DeclareLocal(property.PropertyType);
                if (context.GroBufReader.Options.HasFlag(GroBufOptions.MergeOnRead))
                {
                    MethodInfo getter = property.GetGetMethod(true);
                    if(getter == null)
                        throw new MissingMethodException(Type.Name, property.Name + "_get");
                    il.Ldarg(3); // stack: [data, ref index, dataLength, ref result]
                    if (!Type.IsValueType)
                        il.Ldind(typeof(object)); // stack: [data, ref index, dataLength, result]
                    il.Call(getter, Type); // stack: [ data, ref index, dataLength, result.property]
                    il.Stloc(propertyValue); // propertyValue = result.property; stack: [data, ref index, dataLength]
                }
                il.Ldloca(propertyValue); // stack: [data, ref index, dataLength, ref propertyValue]
                ReaderMethodBuilderContext.CallReader(il, property.PropertyType, context); // reader(data, ref index, dataLength, ref propertyValue); stack: []
                il.Ldarg(3); // stack: [ref result]
                if(!Type.IsValueType)
                    il.Ldind(typeof(object)); // stack: [result]
                il.Ldloc(propertyValue); // stack: [result, propertyValue]
                MethodInfo setter = property.GetSetMethod(true);
                if(setter == null)
                    throw new MissingMethodException(Type.Name, property.Name + "_set");
                il.Call(setter, Type); // result.property = propertyValue
                break;
            default:
                throw new NotSupportedException("Data member of type '" + member.MemberType + "' is not supported");
            }
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(ReaderDelegate<>).MakeGenericType(Type));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }
    }
}