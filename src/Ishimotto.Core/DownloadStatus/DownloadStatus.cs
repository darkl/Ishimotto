namespace Ishimotto.Core.DownloadStatus
{
    public abstract class DownloadStatus<T>
    {
        private readonly T mItem;

        public DownloadStatus(T item)
        {
            mItem = item;
        }

        #region Members

        public T Item
        {
            get { return mItem; }
        }

        #endregion
    }

    public class DownloadStarted<T> : DownloadStatus<T>
    {
        public DownloadStarted(T item) : base(item)
        {
        }

        public string Url { get; set; }

        public string Destination { get; set; }
    }
}