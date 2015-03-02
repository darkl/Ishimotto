using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.Core;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using Ishimotto.NuGet.NuGetGallery;
using log4net;


namespace Ishimotto.NuGet
{
    /// <summary>
    /// An implememntation of <see cref="IAsyncTask"/> to download NuGet dependencies
    /// </summary>
    public class NuGetDownloadAsyncTask : IAsyncTask
    {

        private ILog mLogger;

        private NuGetSettings mSettings;

        private IDependenciesRepostoryInfo mRepositoryInfo;

        private DateTime mLastFetchTime;

        public NuGetDownloadAsyncTask(NuGetSettings settings,IDependenciesRepostoryInfo info, DateTime lastFetchTime)
        {
            mLogger = LogManager.GetLogger(typeof (NuGetDownloadAsyncTask));

            mSettings = settings;

            mRepositoryInfo = info;

            mLastFetchTime = lastFetchTime;
        }

        public async Task ExecuteAsync()
        {
            //var ishimottoSettings = IshimottoConfig.GetConfig();
            
            NuGetQuerier querier = new NuGetQuerier(mSettings.NuGetUrl);

            mLogger.Info("Quering NuGet to get packages inforamtion");

            var packages =
                 querier.FetchFrom(mLastFetchTime);

            Download(packages);

            mLogger.Debug("Finish isimotto process");
        }

        private async  void Download(IEnumerable<V2FeedPackage> packages)
        {
            //Todo: must be a better solution

            AriaDownloader downloader = new AriaDownloader(mSettings.DownloadDirectory,true, 10, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria.log"),
            AriaSeverity.Error);

            
            
            var container = new DependencyContainer(mSettings.RemoteRepositoryUrl, mRepositoryInfo);

            var dtos = packages.Select(p => p.ToDto());

            await container.AddDependencies(dtos);

            mLogger.Info("Resolving dependencis");

            HandleDependencies(dtos, container, downloader).Wait();

            mLogger.Debug("Finish resolve dependencies");

            mLogger.Info("Start downloading packages");

            downloader.Download(dtos.Select(dto => dto.GetDownloadLink()));
        }

        private async  Task HandleDependencies(IEnumerable<PackageDto> packages, DependencyContainer pmDownloader, AriaDownloader downloader)
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

       
    }
}
