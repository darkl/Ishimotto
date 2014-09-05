namespace Ishimotto.Core.Legacy.DownloadStatus
{
    public class DownloadStarted<T> : DownloadStatus<T>
    {
        public DownloadStarted(T item) : base(item)
        {
        }

        public string Url { get; set; }

        public string Destination { get; set; }
    }
}