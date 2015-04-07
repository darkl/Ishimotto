using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ishimotto.Core;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using Ishimotto.NuGet.NuGetGallery;
using log4net;
using Microsoft.Build.Framework;


namespace Ishimotto.NuGet
{
    /// <summary>
    /// An implememntation of <see cref="IAsyncTask"/> to download NuGet dependencies
    /// </summary>
    public class NuGetDownloadAsyncTask : IAsyncTask
    {
        #region Data Members
        /// <summary>
        /// A logger
        /// </summary>
        private ILog mLogger;

        /// <summary>
        /// Setting to initialize the <see cref="NuGetDownloadAsyncTask"/>
        /// </summary>
        private NuGetSettings mSettings;

        /// <summary>
        /// The minimum date to fetch packages from
        /// </summary>
        private DateTime mLastFetchTime;

        /// <summary>
        /// A container to handle dependencies with
        /// </summary>
        /// <remarks>
        /// This container is used to check if a package has dependencies that needed to be downloaded, and if so it update the <see cref="IDependenciesRepostory"/>
        /// about the new dependencies
        /// </remarks>
        private DependencyContainer mDependencyContainer;

        /// <summary>
        /// A dopwnloader to download all packages
        /// </summary>
        /// <remarks>
        /// This downloader uses aria2c to download the packages, so make sure the tool exist in the Bin directory
        /// </remarks>
        private AriaDownloader mDownloader;
        #endregion

        #region Constants
        /// <summary>
        /// The number of packages per page to fetch in the <see cref="NuGetFetcher"/>
        /// </summary>
        private const int DEFAULT_PAGE_SIZE = 20;

        /// <summary>
        /// The maximum number of pages to handle simultaneously
        /// <remarks>
        /// This parameter is used in the tpl blocks initialiation to make sure that they wont ask for packages from the nuget feed, when they have enouth packages to handle
        /// </remarks>
        /// </summary>
        private const int MAX_PAGES_TO_HANDLE = 5; 
        #endregion

        #region Constructor
        /// <summary>
        /// Creates new instance of <see cref="NuGetDownloadAsyncTask"/>
        /// </summary>
        /// <param name="settings">Setting to initialize the <see cref="NuGetDownloadAsyncTask"/></param>
        /// <param name="info">Settings to initialize the <see cref="IDependenciesRepostory"/></param>
        /// <param name="lastFetchTime">The minimum date to fetch packages from</param>
        public NuGetDownloadAsyncTask(NuGetSettings settings, IDependenciesRepostoryInfo info, DateTime lastFetchTime)
        {
            mLogger = LogManager.GetLogger(typeof(NuGetDownloadAsyncTask));

            mSettings = settings;

            mLastFetchTime = lastFetchTime;

            mDependencyContainer = new DependencyContainer(mSettings.RemoteRepositoryUrl, info);

            mDownloader = new AriaDownloader(mSettings.DownloadDirectory, true, 25, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria.log"),
                                                AriaSeverity.Error);
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the <see cref="NuGetDownloadAsyncTask"/> task, download all packages from <see cref="mLastFetchTime"/>
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteAsync()
        {
            NuGetQuerier querier = new NuGetQuerier(mSettings.RemoteRepositoryUrl);

            mLogger.Info("Quering NuGet to get packages inforamtion");

            var packages =
                 querier.FetchFrom(mLastFetchTime, DEFAULT_PAGE_SIZE, TimeSpan.FromSeconds(45)).Select(package => package.ToDto());

            var packageBrodcaster = new BroadcastBlock<PackageDto>(dto => dto);

            var completion = InitTplBlocks(packageBrodcaster);

            foreach (var packageDto in packages)
            {
                packageBrodcaster.Post(packageDto);
            }

            packageBrodcaster.Complete();

            await completion;

            mDownloader.Download();
        } 

        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize all tpl blocks
        /// </summary>
        /// <param name="packageBrodcaster">The first package in the pipe
        /// <remarks>
        /// This package must be created in the <see cref="ExecuteAsync"/> method, to signal completion
        /// </remarks>
        /// </param>
        /// <returns>a Task completed when all the blocks complete</returns>
        private Task InitTplBlocks(BroadcastBlock<PackageDto> packageBrodcaster)
        {
            BatchBlock<PackageDto> dependenciesBatch;
            ActionBlock<PackageDto[]> containerUpdater;
            ActionBlock<PackageDto> dependencyResolver;
            ActionBlock<PackageDto> addPendingDownload;

            dependenciesBatch = new BatchBlock<PackageDto>(DEFAULT_PAGE_SIZE);

            containerUpdater = new ActionBlock<PackageDto[]>(async unit => await mDependencyContainer.AddDependencies(unit));

            dependencyResolver = new ActionBlock<PackageDto>(async package => await ResolveDependnecies(package),
                new ExecutionDataflowBlockOptions { BoundedCapacity = DEFAULT_PAGE_SIZE * MAX_PAGES_TO_HANDLE });

            dependenciesBatch.LinkTo(containerUpdater, new DataflowLinkOptions { PropagateCompletion = true });

            addPendingDownload = new ActionBlock<PackageDto>(package => mDownloader.AddLink(package.GetDownloadLink()),
                new ExecutionDataflowBlockOptions { BoundedCapacity = DEFAULT_PAGE_SIZE * MAX_PAGES_TO_HANDLE });

            packageBrodcaster.LinkTo(dependenciesBatch, new DataflowLinkOptions { PropagateCompletion = true });

            packageBrodcaster.LinkTo(dependencyResolver, new DataflowLinkOptions { PropagateCompletion = true });

            packageBrodcaster.LinkTo(addPendingDownload, new DataflowLinkOptions { PropagateCompletion = true });

            return Task.WhenAll(addPendingDownload.Completion, dependenciesBatch.Completion,
                dependencyResolver.Completion, containerUpdater.Completion);

        }


        /// <summary>
        /// Resolve dependencies of a package
        /// </summary>
        /// <param name="package">The package to resolve dependencies for</param>
        /// <returns>Task completed when all the dependencies of <see cref="package"/> has been resolved</returns>
        private async Task ResolveDependnecies(PackageDto package)
        {
            var dependencies = await mDependencyContainer.GetDependenciesAsync(package, updateRepository: true).ConfigureAwait(false);

            if (dependencies.Any())
            {
                var taskList = new List<Task>();

                mLogger.InfoFormat("Adding Dependencies to download for {0}", package.ID);

                mDownloader.AddLinks(dependencies.Select(d => d.GetDownloadLink()));

                foreach (var dependency in dependencies)
                {
                    taskList.Add(ResolveDependnecies(dependency));
                }

                await Task.WhenAll(taskList).ConfigureAwait(false);
            }
        } 
        #endregion

    }
}
