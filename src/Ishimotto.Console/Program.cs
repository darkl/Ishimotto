﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ishimotto.Core;
using Ishimotto.NuGet;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using Ishimotto.NuGet.NuGetGallery;
using log4net;
using log4net.Config;

namespace Ishimotto.Console
{

    internal class Program
    {
        private static ILog mLogger;

        private static void Main(string[] args)
        {
            InitializeLogger();

            var ishimottoSettings = IshimottoConfig.GetConfig();

            NuGetQuerier querier = new NuGetQuerier(ishimottoSettings.NuGetUrl);

            mLogger.Info("Quering NuGet to get packages inforamtion");

            var result =
                 querier.FetchFrom(ishimottoSettings.LastFetchTime,20,TimeSpan.FromSeconds(40));

           // var links = result.Select(package => NuGetDownloader.GetUri(package.GalleryDetailsUrl));

            //if (mLogger.IsInfoEnabled)
            //{
            //   mLogger.InfoFormat("{0} packages returned", links.Count());
            //}
            
            Download(ishimottoSettings, result);

            mLogger.Debug("Finish isimotto process");
        }

        private async static void Download(IshimottoConfig ishimottoSettings, IEnumerable<V2FeedPackage> result)
        {
            //Todo: must be a better solution

            AriaDownloader downloader = new AriaDownloader(ishimottoSettings.DownloadsDirectory,
                ishimottoSettings.DeleteTempFiles, ishimottoSettings.MaxConnections, ishimottoSettings.AriaLogPath,
            ishimottoSettings.AriaLogLevel);

            //Todo: config
            var mongoRepository = new MongoDepndenciesRepository("mongodb://localhost:27017");

            //Todo:// config
            var container = new DependencyContainer(@"https://packages.nuget.org/api/v2",
                ishimottoSettings.DownloadsDirectory, mongoRepository);

            var dtos = result.Select(p => p.ToDto());
            
            await container.AddDependencies(dtos);

            mLogger.Info("Resolving dependencis");
            
            HandleDependencies(dtos, container, downloader).Wait();

            mLogger.Debug("Finish resolve dependencies");

            mLogger.Info("Start downloading packages");

            downloader.Download(dtos.Select(dto => dto.GetDownloadLink()));
        }

        private async static Task HandleDependencies(IEnumerable<PackageDto> packages, DependencyContainer pmDownloader, AriaDownloader downloader)
        {
            Parallel.ForEach(packages, async package =>
            {
                var dependencies = await pmDownloader.GetDependenciesAsync(package);

                if (dependencies.Count() > 0)
                {
                    mLogger.InfoFormat("Adding Dependencies to download for {0}", package.ID);

                    downloader.AddLinks(dependencies.Select(d => d.GetDownloadLink()));

                    HandleDependencies(dependencies, pmDownloader, downloader);
                }
            });
        }



        private static void InitializeLogger()
        {
            XmlConfigurator.Configure();

            mLogger = LogManager.GetLogger("Ishimotto.Console.Program");

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            mLogger.Debug("Start Ishimotto process");
        }

        /// <summary>
        /// Logging the exception before terminating the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            mLogger.Fatal("An unhandled exception occured", e.ExceptionObject as Exception);
        }



    }
}