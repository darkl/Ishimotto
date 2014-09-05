using System;
using System.Collections.Generic;
using System.Linq;
using Ishimotto.Core.Aria;
using Ishimotto.NuGet;
using Ishimotto.NuGet.NuGetGallery;
using log4net;
using log4net.Config;

namespace Ishimotto.Console
{   
    internal class Program
    {
        private static readonly ILog mLogger = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            mLogger.Debug("Start Ishimotto process");

            IshimottoConfig ishimottoSettings = IshimottoConfig.GetConfig();

            NuGetQuerier querier = new NuGetQuerier(ishimottoSettings.NuGetUrl);

            mLogger.Info("quering NuGet to get packages inforamtion");

            IEnumerable<V2FeedPackage> result =
                querier.FetchFrom(ishimottoSettings.LastFetchTime);

            List<string> links = 
                result.Select(package => NuGetDownloader.GetUri(package.GalleryDetailsUrl)).ToList();

            mLogger.InfoFormat("{0} packages returned", links.Count());

            mLogger.Info("Start downloading packages");

            AriaDownloader downloader = new AriaDownloader(ishimottoSettings.DownloadsDirectory, ishimottoSettings.DeleteTempFiles, ishimottoSettings.MaxConnections, ishimottoSettings.AriaLogPath, ishimottoSettings.AriaLogLevel);

            downloader.Download(links);

            mLogger.Debug("Finish isimotto process");

            ishimottoSettings.LastFetchTime = DateTime.Now;
        }

        /// <summary>
        /// Logging the exception before terminating the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            mLogger.Fatal("An unhandled exception occured",e.ExceptionObject as Exception);
        }
    }
}