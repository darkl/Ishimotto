using System;
using System.IO;
using System.Linq;
using System.Text;
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

              var loggerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IshimottoSettings.Default.LoggerFileName);

             XmlConfigurator.ConfigureAndWatch(new FileInfo(loggerPath));


            logger = LogManager.GetLogger("Ishimotto.Console.Program");

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
            
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Start Ishimotto process");
            }

            var ishimottoSettings = IshimottoSettings.Default;

            NuGetQuerier querier = new NuGetQuerier(ishimottoSettings.NuGetUrl);


            if (logger.IsInfoEnabled)
            {
                logger.Info("quering NuGet to get packages inforamtion");
            }

            var result =
                querier.FetchFrom(ishimottoSettings.LastFetchTime);


            var links = result.Select(package => NuGetDownloader.GetUri(package.GalleryDetailsUrl));

            if (logger.IsInfoEnabled)
            {
                logger.InfoFormat("{0} packages returned", links.Count());
            }

            var severity = GetAriaSeverity();

            if (logger.IsInfoEnabled)
            {
                logger.Info("Start downloading packages");
            }

            AriaDownloader downloader = new AriaDownloader(ishimottoSettings.DownloadDirectory, ishimottoSettings.DeleteTempFile, ishimottoSettings.MaxConnections, ishimottoSettings.AriaLogPath, severity);


            downloader.Download(links);

            if (logger.IsDebugEnabled)
            {
                logger.Debug("Finish isimotto process");
            }


            IshimottoSettings.Default.LastFetchTime = DateTime.Now;

            IshimottoSettings.Default.Save();

        }

        /// <summary>
        /// Logging the exception before terminating the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal("An unhandled exception occured",e.ExceptionObject as Exception);
        }

        /// <summary>
        /// Get AriaSeverity from the configuration
        /// </summary>
        /// <returns><see cref="AriaSeverity"/></returns>
        private static AriaSeverity GetAriaSeverity()
        {

            if (logger.IsDebugEnabled)
            {
                logger.Debug("Choosing AriaSeverity");
            }

            AriaSeverity severity;

            var ishimottoSettings = IshimottoSettings.Default;

            if (!AriaSeverity.TryParse(ishimottoSettings.AriaLogLevel, out severity))
            {
                if (logger.IsWarnEnabled)
                {
                    LogAriaSeverityWarning();
                }

                severity = AriaSeverity.Error;
            }
            return severity;
        }

        /// <summary>
        /// Logs that the AriaSeverity parameter can not be parsed and the ERROR level would be taken
        /// </summary>
        private static void LogAriaSeverityWarning()
        {
            StringBuilder warnMessage =
                new StringBuilder(
                    "Could not parse AriaSeverity from configuratoin, the AriaSeverity value must be one of the following: \n");

            warnMessage.Append(AriaSeverity.Debug);
            warnMessage.Append(",");

            warnMessage.Append(AriaSeverity.Info);
            warnMessage.Append(",");

            warnMessage.Append(AriaSeverity.Notice);
            warnMessage.Append(",");

            warnMessage.Append(AriaSeverity.Warn);
            warnMessage.Append(",");

            warnMessage.Append(AriaSeverity.Error);
            warnMessage.Append(",");

            warnMessage.Append("The level ERROR would be taken to log aria process");

            logger.Warn(warnMessage.ToString());
        }
        

    }
}