using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using log4net;
using NuGet;

namespace Ishimotto.NuGet
{
    /// <summary>
    /// Downloader for NuGet packages 
    /// </summary>
    /// <remarks>
    /// This downaloader download's packages (including their dependencies) using the NuGet's <see cref="PackageManager"/>
    /// </remarks>
    public class PackageManagerDownloader
    {

        #region Data Members
        /// <summary>
        /// Connects to the NuGet repository and download the packages
        /// </summary>
        private PackageManager mPackageManager;

        /// <summary>
        /// Dependencies repository to prevent download of unnecessary packages
        /// </summary>
        public IDependenciesRepostory DependenciesRepostory { get; internal set; }

        /// <summary>
        /// Path to the repository of the downloaded packages
        /// </summary>
        private string mLocalRepository;

        private ILog mLogger;

        #endregion

        #region Consts

        /// <summary>
        /// Perfix for the working directory (local to the <see cref="mLocalRepository"/>
        /// </summary>
        private const string TEMP_DIRECTORY_NAME = "Temp";

        /// <summary>
        /// Max retries to download a package
        /// </summary>
        private const int MAX_RETRIES = 5;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates new instance of <see cref="PackageManagerDownloader"/>
        /// </summary>
        /// <param name="nugetRepository">path to the source NuGet repository (NuGet website)</param>
        /// <param name="localRepository">path to the destenation repository</param>
        /// <param name="dependenciesRepostory">Entity to check if package's depndencies are needed</param>
        public PackageManagerDownloader(string nugetRepository, string localRepository, IDependenciesRepostory dependenciesRepostory)
        {
            mLogger = LogManager.GetLogger(typeof(PackageManagerDownloader).Name);

            mPackageManager = new PackageManager(PackageRepositoryFactory.Default.CreateRepository(nugetRepository), Path.Combine(localRepository, TEMP_DIRECTORY_NAME));

            mPackageManager.LocalRepository.PackageSaveMode = PackageSaveModes.Nupkg;

            mPackageManager.PackageInstalled += DownloadDependencies;

            DependenciesRepostory = dependenciesRepostory;

            mLocalRepository = localRepository;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Download packages including their dependencies
        /// </summary>
        /// <param name="packages">Packages to download</param>
        public async Task DownloadPackagesAsync(IEnumerable<PackageDto> packages)
        {
            await DependenciesRepostory.AddDepndenciesAsync(packages);

            mLogger.Info("Start downloading packages");

            if (mLogger.IsDebugEnabled)
            {
                mLogger.DebugFormat("Downloading the following packages : {0}", String.Join(", ", packages.Select(package => package.ID)));
            }

            Parallel.ForEach(packages, package => DownloadPackageAsync(package, false));
        }

        public async Task DownloadPackageAsync(PackageDto package, bool addToRepository = true)
        {
            if (addToRepository)
            {
                await DependenciesRepostory.AddDependnecyAsync(package);
            }

            var attempt = 1;

            bool isDownloadComplete = false;

            while (!isDownloadComplete && attempt < MAX_RETRIES)
            {

                try
                {
                    mLogger.Info("Downloading package: " + package.ID);

                    mPackageManager.InstallPackage(package.ID, package.Version, true, false);
                    isDownloadComplete = true;
                }

                catch (Exception ex)
                {
                    string errorMessage = "Failed to download package due to unknown error, tring again in 5 seconds";

                    if (attempt < MAX_RETRIES)
                    {
                        errorMessage += ", try again in 5 seconds";
                    }

                    mLogger.Warn(errorMessage);

                    Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                }
            }

            if (attempt == MAX_RETRIES)
            {
                mLogger.Error("Failed to download package: " + package.ID);
            }
        }

        // Todo: Implement IDisposable that return task????
        public async Task Dispose()
        {
            mPackageManager.PackageInstalled -= DownloadDependencies;

            await CopyFilesToLocalRepository();

            Directory.Delete(mPackageManager.LocalRepository.Source, true);
        }

        /// <summary>
        /// Download packages from a specific time
        /// </summary>
        /// <param name="fetchFrom">The time to download packages from</param>
        public async Task DownloadPackagesAsync(DateTime fetchFrom)
        {
            mLogger.InfoFormat("Downloading packages from {0}", fetchFrom.ToShortDateString());

            var packages = from package in mPackageManager.SourceRepository.GetPackages()
                           where package.IsLatestVersion &&
                                 package.Listed && package.Published.HasValue &&
                                 package.Published.Value.DateTime.CompareTo(fetchFrom) >= 0
                           select package.ToDto();

            await DownloadPackagesAsync(packages);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Downloads the dependencies of a package
        /// </summary>
        /// <param name="sender"><see cref="mPackageManager"/></param>
        /// <param name="e">Event containing the downloaded package</param>
        internal async void DownloadDependencies(object sender, PackageOperationEventArgs e)
        {
            var dependnciesToDownload = from set in e.Package.DependencySets
                                        from depndency in set.Dependencies
                                        where DependenciesRepostory.ShouldDownload(depndency)
                                        select depndency.ToDto(mPackageManager);
            
            if (!dependnciesToDownload.IsEmpty())
            {

                await DownloadPackagesAsync(dependnciesToDownload);
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Copy the packages from the working directory to <see cref="mLocalRepository"/>
        /// </summary>
        /// <returns></returns>
        private Task CopyFilesToLocalRepository()
        {
            mLogger.Info("Copying packages from Temp folder to local repository");

            return Task.Run(() =>
            {
                var files = new DirectoryInfo(mPackageManager.LocalRepository.Source).EnumerateFiles("*.nupkg",
                    SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    file.CopyTo(Path.Combine(mLocalRepository, file.Name));
                }

            });

        }

        #endregion
    }

}
