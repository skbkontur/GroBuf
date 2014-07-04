namespace GroBuf
{
    public class ReaderContext
    {
        public ReaderContext(int length, int numberOfObjects)
        {
            this.length = length;
            objects = numberOfObjects == 0 ? null : new object[numberOfObjects];
        }

        public readonly int length;
        public int count;
        public readonly object[] objects;
    }
}