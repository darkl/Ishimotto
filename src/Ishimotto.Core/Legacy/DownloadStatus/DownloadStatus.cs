namespace Ishimotto.Core.Legacy.DownloadStatus
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
}