namespace Ishimotto.Core.Legacy
{
    public class PendingDownload<T>
    {
        private readonly T mItem;

        public PendingDownload(T item)
        {
            mItem = item;
        }

        public int NumberOfTries { get; set; }
        
        public bool Succeeded { get; set; }

        public T Item
        {
            get
            {
                return mItem;
            }
        }
    }
}