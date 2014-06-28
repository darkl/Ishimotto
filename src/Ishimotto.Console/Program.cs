using System;
using System.IO;
using System.Linq;
using Ishimotto.Core;
using Ishimotto.NuGet;
using Ishimotto.NuGet.NuGetGallery;

namespace Ishimotto.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            NuGetQuerier querier = new NuGetQuerier();

            var result =
                querier.FetchEverything(40, TimeSpan.FromSeconds(10));


            var links = result.Select(package => NuGetDownloader.GetUri(package.GalleryDetailsUrl));

            var ishimottoSettings = IshimottoSettings.Default;


            AriaSeverity severity;


            if (!AriaSeverity.TryParse(ishimottoSettings.AriaLogLevel, out severity))
            {
                //TODO: log that the severity would be set to error

                severity = AriaSeverity.Error;
                
            }

            AriaDownloader downloader = new AriaDownloader(ishimottoSettings.DownloadDirectory,ishimottoSettings.DeleteTempFile,ishimottoSettings.MaxConnections,ishimottoSettings.AriaLogPath,severity);


            downloader.Download(links);


            //foreach (V2FeedPackage package in result.Take(5))
            //{
            //    File.WriteAllText(Guid.NewGuid() + ".txt",
            //                      FormatPackage(package));
            //}
        }

        public static string FormatPackage(V2FeedPackage package)
        {
            return "Id:" + package.Id + Environment.NewLine +
                   "Title: " + package.Title + Environment.NewLine +
                   "DownloadCount:" + package.DownloadCount + Environment.NewLine +
                   "Authors: " + package.Authors + Environment.NewLine +
                   "Version: " + package.Version + Environment.NewLine +
                   "Dependencies: " + package.Dependencies + Environment.NewLine +
                   "Tags:" + package.Tags;
        }

   
    }
}