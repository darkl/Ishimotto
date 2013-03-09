namespace Ishimotto.Core.DownloadStatus
{
    public class DownloadComplete<T> : DownloadStatus<T>
    {
        public DownloadComplete(T item) : base(item)
        {
        }
    }
}