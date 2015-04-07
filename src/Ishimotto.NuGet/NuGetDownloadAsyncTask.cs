using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

        private AriaDownloader mDownloader;
        
        private const int DEFAULT_PAGE_SIZE = 20;

        public NuGetDownloadAsyncTask(NuGetSettings settings, IDependenciesRepostoryInfo info, DateTime lastFetchTime)
        {
            mLogger = LogManager.GetLogger(typeof(NuGetDownloadAsyncTask));

            mSettings = settings;

            mLastFetchTime = lastFetchTime;

            mDependencyContainer = new DependencyContainer(mSettings.RemoteRepositoryUrl, info);

            mDownloader = new AriaDownloader(mSettings.DownloadDirectory, true, 10, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria.log"),
AriaSeverity.Error);
        }

        public async Task ExecuteAsync()
        {
            //var ishimottoSettings = IshimottoConfig.GetConfig();

            NuGetQuerier querier = new NuGetQuerier(mSettings.RemoteRepositoryUrl);

            mLogger.Info("Quering NuGet to get packages inforamtion");

            //Getting the must download packages from the configuration
            //var prerealsePackage = querier.FetchFrom(mSettings.Prerelase.ToList(), mLastFetchTime, DEFAULT_PAGE_SIZE,
            //    TimeSpan.FromSeconds(10), true).Select(package => package.ToDto());

            var packages =
                 querier.FetchFrom(mLastFetchTime, DEFAULT_PAGE_SIZE, TimeSpan.FromSeconds(45)).Select(package => package.ToDto());

            //   packages = packages.Concat(prerealsePackage);


            BatchBlock<PackageDto> dependenciesBatch = new BatchBlock<PackageDto>(DEFAULT_PAGE_SIZE);

            ActionBlock<PackageDto[]> addPackagesToContainer = new ActionBlock<PackageDto[]>(async unit =>await mDependencyContainer.AddDependencies(unit));

            ActionBlock<PackageDto> handleDependencies = new ActionBlock<PackageDto>(async package => await HandleDependencies(package),
            new ExecutionDataflowBlockOptions { BoundedCapacity = DEFAULT_PAGE_SIZE * 5});

            dependenciesBatch.LinkTo(addPackagesToContainer);

            ActionBlock<PackageDto> addPendingDownloadBlock = new ActionBlock<PackageDto>(package => mDownloader.AddLink(package.GetDownloadLink()), new ExecutionDataflowBlockOptions { BoundedCapacity = DEFAULT_PAGE_SIZE * 5 });
            
            int packageIndex = 0;

            foreach (var packageDto in packages)
            {
                dependenciesBatch.Post(packageDto);
                handleDependencies.Post(packageDto);
                addPendingDownloadBlock.Post(packageDto);

                //if (packageIndex > 0 && packageIndex > 5* DEFAULT_PAGE_SIZE && packageIndex % DEFAULT_PAGE_SIZE == 0)
                //{
                //    await PageProcessingCompletetion(handleDependencies, addPendingDownloadBlock);
                //}
                packageIndex++;
            }
            
            handleDependencies.Complete();

            dependenciesBatch.TriggerBatch();

            dependenciesBatch.Complete();

           addPendingDownloadBlock.Complete();

            //Todo: see if it is necessary (check if a batch block signals completion, do the attached blocks complete too)
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            addPackagesToContainer.Complete();

            await Task.WhenAll(addPendingDownloadBlock.Completion, dependenciesBatch.Completion, handleDependencies.Completion, addPackagesToContainer.Completion).ConfigureAwait(false);

            mDownloader.Download();
        }

        private async Task PageProcessingCompletetion( ActionBlock<PackageDto> handleDependencies, ActionBlock<PackageDto> addPendingDownloadBlock)
        {
            while (handleDependencies.InputCount > 0 || addPendingDownloadBlock.InputCount > 0)
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
        }

        private void Download(IEnumerable<PackageDto> packages)
        {
            mDownloader.Download(packages.Select(dto => dto.GetDownloadLink()));
        }

        private async Task HandleDependencies(IEnumerable<PackageDto> packages, DependencyContainer pmDownloader, AriaDownloader downloader)
        {
            //Parallel.ForEach(packages, async package =>

            foreach (var package in packages)
            {
                var dependencies = await pmDownloader.GetDependenciesAsync(package);

                if (dependencies.Any())
                {
                    mLogger.InfoFormat("Adding Dependencies to download for {0}", package.ID);

                    downloader.AddLinks(dependencies.Select(d => d.GetDownloadLink()));

                    HandleDependencies(dependencies, pmDownloader, downloader);
                }
            }
        }

        private async Task HandleDependencies(PackageDto package)
        {
            var dependencies = await mDependencyContainer.GetDependenciesAsync(package,updateRepository:true).ConfigureAwait(false);

            if (dependencies.Any())
            {
                var taskList = new List<Task>();

                mLogger.InfoFormat("Adding Dependencies to download for {0}", package.ID);

                mDownloader.AddLinks(dependencies.Select(d => d.GetDownloadLink()));

                foreach (var dependency in dependencies)
                {
                    taskList.Add(HandleDependencies(dependency));
                }

                await Task.WhenAll(taskList).ConfigureAwait(false);
            }
        }

    }
}
