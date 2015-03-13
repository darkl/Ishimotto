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

        private DateTime mLastFetchTime;

        private DependencyContainer mDependencyContainer;

        private const int DEFAULT_PAGE_SIZE = 20;

        public NuGetDownloadAsyncTask(NuGetSettings settings,IDependenciesRepostoryInfo info, DateTime lastFetchTime)
        {
            mLogger = LogManager.GetLogger(typeof (NuGetDownloadAsyncTask));

            mSettings = settings;

            mLastFetchTime = lastFetchTime;

            mDependencyContainer = new DependencyContainer(mSettings.RemoteRepositoryUrl, info);
        }

        public async Task ExecuteAsync()
        {
            //var ishimottoSettings = IshimottoConfig.GetConfig();
            
            NuGetQuerier querier = new NuGetQuerier(mSettings.RemoteRepositoryUrl);

            mLogger.Info("Quering NuGet to get packages inforamtion");

            //Getting the must download packages from the configuration
            var prerealsePackage = querier.FetchFrom(mSettings.Prerelase, mLastFetchTime, DEFAULT_PAGE_SIZE,
                TimeSpan.FromSeconds(10),true).Select(package => package.ToDto());

            var packages =
                 querier.FetchFrom(mLastFetchTime,DEFAULT_PAGE_SIZE,TimeSpan.FromSeconds(60)).Select(package => package.ToDto());

            packages = packages.Concat(prerealsePackage);

            var updateTask =  mDependencyContainer.AddDependencies(packages);

            Download(packages);

            await updateTask;

        }

        private async  void Download(IEnumerable<PackageDto> packages)
        {
            //Todo: must be a better solution

            AriaDownloader downloader = new AriaDownloader(mSettings.DownloadDirectory,true, 10, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria.log"),
            AriaSeverity.Error);
           
         
            mLogger.Info("Resolving dependencis");

           await HandleDependencies(packages, mDependencyContainer, downloader);

            mLogger.Debug("Finish resolve dependencies");

            mLogger.Info("Start downloading packages");

            downloader.Download(packages.Select(dto => dto.GetDownloadLink()));
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
