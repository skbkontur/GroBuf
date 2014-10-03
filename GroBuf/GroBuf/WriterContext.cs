using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf
{
    public class WriterContext
    {
        public WriterContext(int length, int start)
        {
            this.length = length;
            this.start = start;
        }

        public int length;
        public int start;
        public int index;
        public int references;
        public readonly Dictionary<object, int> objects = new Dictionary<object, int>();

        public static readonly FieldInfo IndexField = (FieldInfo)((MemberExpression)((Expression<Func<WriterContext, int>>)(context => context.index)).Body).Member;
        public static readonly FieldInfo StartField = (FieldInfo)((MemberExpression)((Expression<Func<WriterContext, int>>)(context => context.start)).Body).Member;
        public static readonly FieldInfo ReferencesField = (FieldInfo)((MemberExpression)((Expression<Func<WriterContext, int>>)(context => context.references)).Body).Member;
        public static readonly FieldInfo ObjectsField = (FieldInfo)((MemberExpression)((Expression<Func<WriterContext, Dictionary<object, int>>>)(context => context.objects)).Body).Member;
    }
}