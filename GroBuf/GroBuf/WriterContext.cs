using System.Collections;

namespace GroBuf
{
    public class WriterContext
    {
        public WriterContext(int length)
        {
            this.length = length;
        }

        public readonly int length;
        public readonly Hashtable objects = new Hashtable();
    }
}