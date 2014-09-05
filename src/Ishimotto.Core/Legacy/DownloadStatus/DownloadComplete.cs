namespace Ishimotto.Core.Legacy.DownloadStatus
{
    public class DownloadComplete<T> : DownloadStatus<T>
    {
        public DownloadComplete(T item) : base(item)
        {
        }
    }
}