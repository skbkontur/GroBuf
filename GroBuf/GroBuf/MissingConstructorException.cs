using System;
using System.Linq;

namespace SKBKontur.GroBuf
{
    public class MissingConstructorException : Exception
    {
        public MissingConstructorException(Type type)
            : base("Type " + type.Name + "has no parameterless constructor")
        {
        }

        public MissingConstructorException(Type type, params Type[] parameters)
            : base(string.Format("Type {0} has no constructor with arguments ({1})", type.Name, string.Join(", ", parameters.Select(t => t.Name))))
        {
        }
    }
}