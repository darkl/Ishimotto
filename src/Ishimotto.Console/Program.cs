using System;
using System.Collections.Generic;
using Ishimotto.Core.DownloadStatus;
using Ishimotto.NuGet;
using Ishimotto.NuGet.NuGetGallery;

namespace Ishimotto.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            NuGetQuerier querier = new NuGetQuerier();

            NuGetDownloader downloader =
                new NuGetDownloader(querier.FetchFrom(TimeSpan.FromDays(1)),
                                    "nupkgs",
                                    30,
                                    0);

            IObservable<DownloadStatus<V2FeedPackage>> listener =
                downloader.Download();

            bool done = false;

            listener.Subscribe(x => HandleMessage((dynamic)x),
                               () =>
                                   {
                                       done = true;
                                       System.Console.WriteLine("Done!");
                                   });

            while (!done)
            {
                System.Console.ReadLine();
            }
        }

        private static void HandleMessage(DownloadStarted<V2FeedPackage> message)
        {
            System.Console.WriteLine("Started downloading " + message.Item.Id +
                                     " from " + message.Url + " to " + message.Destination);
        }

        private static void HandleMessage(DownloadFailed<V2FeedPackage> message)
        {
            System.Console.WriteLine("Failed downloading " + message.Item.Id +
                                     " error: " + message.Exception + ". Try: " + message.NumberOfRetries);
        }

        private static void HandleMessage(DownloadComplete<V2FeedPackage> message)
        {
            System.Console.WriteLine("Finished downloading " + message.Item.Id);
        }
    }
}
