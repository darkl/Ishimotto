using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Ishimotto.Core.DownloadStatus;

namespace Ishimotto.Core
{
    public abstract class Downloader<T>
    {
        private readonly ICollection<T> mDownloadItems;
        private readonly string mDownloadPath;
        private readonly int mParallelDownloads;
        private readonly int mNumberOfRetries;

        private int mTotalFinished = 0;

        private readonly Subject<PendingDownload<T>> mPending = new Subject<PendingDownload<T>>();

        public Downloader(IEnumerable<T> downloadLinks,
                          string downloadPath,
                          int parallelDownloads,
                          int numberOfRetries)
        {
            // TODO: Maybe don't make it a list in the ctor.
            mDownloadItems = downloadLinks.ToList();

            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }

            mDownloadPath = downloadPath;

            mParallelDownloads = parallelDownloads;
            mNumberOfRetries = numberOfRetries;
        }

        public IObservable<DownloadStatus<T>> Download()
        {
            ReplaySubject<DownloadStatus<T>> result = new ReplaySubject<DownloadStatus<T>>();

            var downloadProcess =
                (from item in mPending
                 from status in DownloadItem(item, result).ToObservable()
                 select item).Publish();

            downloadProcess.Connect();

            var pending =
                from item in mDownloadItems.Skip(mParallelDownloads).ToObservable()
                    .Zip(downloadProcess, (pend, finished) => pend)
                select new PendingDownload<T>(item);

            pending.Subscribe(x => mPending.OnNext(x));

            downloadProcess.Subscribe
                (x =>
                     {
                         if (x.Succeeded)
                         {
                             result.OnNext(new DownloadComplete<T>(x.Item));
                         }
                         
                         if (x.Succeeded || x.NumberOfTries == mNumberOfRetries)
                         {
                             Interlocked.Increment(ref mTotalFinished);
                         }

                         if (mTotalFinished == mDownloadItems.Count)
                         {
                             mPending.OnCompleted();
                             result.OnCompleted();
                         }
                     });

            foreach (T item in mDownloadItems.Take(mParallelDownloads))
            {
                mPending.OnNext(new PendingDownload<T>(item));
            }

            return result;
        }

        private async Task DownloadItem(PendingDownload<T> pending, ISubject<DownloadStatus<T>> log)
        {
            WebClient client = new WebClient();

            string downloadLink = GetDownloadLink(pending.Item);

            string destination =
                Path.Combine(mDownloadPath, GetDownloadFileName(pending.Item));

            log.OnNext(new DownloadStarted<T>(pending.Item)
                           {
                               Destination = destination,
                               Url = downloadLink
                           });

            pending.NumberOfTries++;

            try
            {
                await client.DownloadFileTaskAsync(downloadLink, destination).ConfigureAwait(false);
                pending.Succeeded = true;
            }
            catch (Exception ex)
            {
                log.OnNext(new DownloadFailed<T>(pending.Item)
                               {
                                   Exception = ex,
                                   NumberOfRetries = pending.NumberOfTries
                               });

                mPending.OnNext(pending);
            }
        }

        protected abstract string GetDownloadLink(T item);

        protected abstract string GetDownloadFileName(T item);
    }
}