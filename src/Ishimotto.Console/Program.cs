using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Ishimotto.Core;
using Ishimotto.NuGet;
using Ishimotto.NuGet.NuGetGallery;
using log4net;
using log4net.Config;


namespace Ishimotto.Console
{

    internal class Program
    {
        private static ILog logger;

        private static void Main(string[] args)
        {

            XmlConfigurator.Configure();

            logger = LogManager.GetLogger("Ishimotto.Console.Program");

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            logger.Debug("Start Ishimotto process");

            var ishimottoSettings = IshimottoConfig.GetConfig();



            NuGetQuerier querier = new NuGetQuerier(ishimottoSettings.NuGetUrl);

            logger.Info("quering NuGet to get packages inforamtion");
            
            var result =
                 querier.FetchFrom(ishimottoSettings.LastFetchTime);


        var links = result.Select(package => NuGetDownloader.GetUri(package.GalleryDetailsUrl));

            if (logger.IsInfoEnabled)
            {
                logger.InfoFormat("{0} packages returned", links.Count());
            }

            logger.Info("Start downloading packages");
            
            AriaDownloader downloader = new AriaDownloader(ishimottoSettings.DownloadsDirectory, ishimottoSettings.DeleteTempFiles, ishimottoSettings.MaxConnections, ishimottoSettings.AriaLogPath, ishimottoSettings.AriaLogLevel);


            downloader.Download(links);

            if (logger.IsDebugEnabled)
            {
                logger.Debug("Finish isimotto process");
            }


            ishimottoSettings.LastFetchTime = DateTime.Now;

        }

        /// <summary>
        /// Logging the exception before terminating the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal("An unhandled exception occured", e.ExceptionObject as Exception);
        }



    }
}