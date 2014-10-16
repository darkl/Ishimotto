using System;

namespace Ishimotto.Core.Legacy.DownloadStatus
{
    public class DownloadFailed<T> : DownloadStatus<T>
    {
        public DownloadFailed(T item) : base(item)
        {
        }

        public int NumberOfRetries { get; set; }
        public Exception Exception { get; set; }
    }
}