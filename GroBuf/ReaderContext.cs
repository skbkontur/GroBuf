using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf
{
    public class ReaderContext
    {
        public ReaderContext(long serializerId, int length, int start, int references)
        {
            this.serializerId = serializerId;
            this.length = length;
            this.start = start;
            objects = references == 0 ? null : new Dictionary<int, object>();
        }

        public readonly long serializerId;
        public readonly int length;
        public readonly int start;
        public readonly Dictionary<int, object> objects;

        public static readonly FieldInfo LengthField = (FieldInfo)((MemberExpression)((Expression<Func<ReaderContext, int>>)(context => context.length)).Body).Member;
        public static readonly FieldInfo StartField = (FieldInfo)((MemberExpression)((Expression<Func<ReaderContext, int>>)(context => context.start)).Body).Member;
        public static readonly FieldInfo ObjectsField = (FieldInfo)((MemberExpression)((Expression<Func<ReaderContext, Dictionary<int, object>>>)(context => context.objects)).Body).Member;
        public static readonly FieldInfo SerializerIdField = (FieldInfo)((MemberExpression)((Expression<Func<ReaderContext, long>>)(context => context.serializerId)).Body).Member;
    }
}