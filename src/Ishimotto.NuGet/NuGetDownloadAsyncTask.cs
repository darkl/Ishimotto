using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ishimotto.Core;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using log4net;

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
        private INuGetSettings mSettings;

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
        private IDependenciesContainer mDependenciesContainer;

        /// <summary>
        /// A dopwnloader to download all packages
        /// </summary>
        /// <remarks>
        /// This downloader uses aria2c to download the packages, so make sure the tool exist in the Bin directory
        /// </remarks>
        private IAriaDownloader mDownloader;

        private int mPackageIndex;


        private Action<string> mUpdateStatusAction;
        private object mUpdateStatus;

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

        /// <summary>
        /// Timeout to fetch a NuGet page
        /// </summary>
        private const int FETCH_TIMEOUT_MILLI = 45 * 1000;

        private const int UPDATE_RATE = 5;

        #endregion

        #region Constructor
        /// <summary>
        /// Creates new instance of <see cref="NuGetDownloadAsyncTask"/>
        /// </summary>
        /// <param name="settings">Setting to initialize the <see cref="NuGetDownloadAsyncTask"/></param>
        /// <param name="info">Settings to initialize the <see cref="IDependenciesRepostory"/></param>
        /// <param name="lastFetchTime">The minimum date to fetch packages from</param>
        public NuGetDownloadAsyncTask(NuGetSettings settings, IDependenciesRepostoryInfo info, DateTime lastFetchTime)
            : this(settings, new DependenciesContainer(settings.RemoteRepositoryUrl, info))
        {
            mLastFetchTime = lastFetchTime;
        }


        private NuGetDownloadAsyncTask(INuGetSettings settings, IDependenciesContainer container)
            : this(settings, container, new AriaDownloader(settings.DownloadDirectory, true, 16, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria.log"),
                AriaSeverity.Error))
        {
            mUpdateStatusAction = Console.WriteLine;
        }

        public NuGetDownloadAsyncTask(INuGetSettings settings, DateTime lastFetchTime, Action<string> statusUpdater)
            : this(settings, new EmptyDependenciesContainer(settings.RemoteRepositoryUrl))
        {
            mLastFetchTime = lastFetchTime;
            mUpdateStatusAction = statusUpdater;
        }


        public NuGetDownloadAsyncTask(INuGetSettings settings, IDependenciesContainer container, IAriaDownloader downloader)
        {
            mLogger = LogManager.GetLogger(typeof(NuGetDownloadAsyncTask));

            mDependenciesContainer = container;

            mSettings = settings;

            mDownloader = downloader;
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


            mUpdateStatusAction("Retrieving Packages from: " + mLastFetchTime);

            mLogger.Info("Quering NuGet to get packages inforamtion");

            var packages =
                 querier.FetchFrom(mLastFetchTime, DEFAULT_PAGE_SIZE, TimeSpan.FromSeconds(FETCH_TIMEOUT_MILLI)).Select(package => package.ToDto());


            packages = packages.Concat(querier.FetchSpecificFrom(mSettings.Prerelase, mLastFetchTime, DEFAULT_PAGE_SIZE,
                TimeSpan.FromSeconds(45)).Select(pkg => pkg.ToDto()));

            var packageBrodcaster = new BroadcastBlock<PackageDto>(dto => dto);

            var completion = InitTplBlocks(packageBrodcaster);

            mLogger.Info("Resolving packgaes Dependencies");


            mUpdateStatusAction("Resolving dependencies");

            foreach (var packageDto in packages)
            {
                packageBrodcaster.Post(packageDto);
            }

            packageBrodcaster.Complete();

            await completion.ConfigureAwait(false);

            mLogger.Debug("Finsih resolving packgaes Dependencies");

            mUpdateStatusAction("Downloading packages");

            mLogger.Info("Downloading packages");

            mDownloader.Download();

            mUpdateStatusAction("Finish Download");
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

            containerUpdater = new ActionBlock<PackageDto[]>((Func<PackageDto[], Task>)mDependenciesContainer.AddDependencies);

            dependencyResolver = new ActionBlock<PackageDto>((Func<PackageDto, Task>)ResolveDependnecies,
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
        internal async Task ResolveDependnecies(PackageDto package)
        {
            var dependencies = await mDependenciesContainer.GetDependenciesAsync(package, updateRepository: true).ConfigureAwait(false);

            UpdateStatus();

            if (dependencies.Any())
            {
                mLogger.InfoFormat("Adding Dependencies to download for {0}", package.ID);

                mDownloader.AddLinks(dependencies.Select(d => d.GetDownloadLink()));

                Subject<PackageDto> dependenciesSubject = new Subject<PackageDto>();

                dependenciesSubject.Subscribe(dependency => ResolveDependnecies(dependency),
                    () => mLogger.Info("All dependencies of " + package.ID + " have been resolved"));

                foreach (var dependency in dependencies)
                {
                    dependenciesSubject.OnNext(dependency);
                }

                dependenciesSubject.OnCompleted();

                dependenciesSubject.LastOrDefault();

            }
        }

        private void UpdateStatus()
        {
            var index = Interlocked.Increment(ref mPackageIndex);

            if (index % UPDATE_RATE == 0)
            {
                mUpdateStatusAction(string.Format("Resolved {0} packages", index));
            }
        }

        #endregion


    }
}
