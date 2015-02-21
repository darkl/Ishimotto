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

        //Todo: create a const named TEMP

        #region Data Members
        /// <summary>
        /// Connects to the NuGet repository and download the packages
        /// </summary>
        private PackageManager mPackageManager;

        /// <summary>
        /// Dependencies repository to prevent download of unnecessary packages
        /// </summary>
        public IDependenciesRepostory DependenciesRepostory { get; internal set; }

        private ILog mLogger;
        
        private string mLocalRepository;



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

            mPackageManager = new PackageManager(PackageRepositoryFactory.Default.CreateRepository(nugetRepository), Path.Combine(localRepository, "Temp"));

            mPackageManager.LocalRepository.PackageSaveMode = PackageSaveModes.Nupkg;

            mPackageManager.PackageInstalled += DownloadDependencies;

            DependenciesRepostory = dependenciesRepostory;

            mLocalRepository = localRepository;
        }


        internal async void DownloadDependencies(object sender, PackageOperationEventArgs e)
        {
            var dependnciesToDownload = from set in e.Package.DependencySets
                                        from depndency in set.Dependencies
                                        where DependenciesRepostory.ShouldDownload(depndency)
                                        select depndency.ToDto();

            if (!dependnciesToDownload.IsEmpty())
            {
                
                await DownloadPackagesAsync(dependnciesToDownload);
            }
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
                mLogger.DebugFormat("Downloading the following packages : {0}",String.Join(", ",packages.Select(package => package.ID)));
            }

            //foreach (var package in packages)
            //{
            //    mPackageManager.InstallPackage(package.ID, package.Version, true, false);
            //}

            Parallel.ForEach(packages, package => DownloadPackageAsync(package, false));

        }

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

        public async Task DownloadPackageAsync(PackageDto package, bool addToRepository = true)
        {
            
            if (addToRepository)
            {
                await DependenciesRepostory.AddDependnecyAsync(package);
            }

            var times = 0;

            bool isDownloadComplete = false;

            while (!isDownloadComplete && times < 5)
            {
                try
                {
                    mLogger.Debug("Downloading package: " + package.ID);

                    mPackageManager.InstallPackage(package.ID, package.Version, true, false);
                    isDownloadComplete = true;
                }
                catch (WebException) //The PackageManagerDownloader sometimes loose connection with the remote host, I saperate the exception in order to keep the logs clean
                {
                    times++;

                    var message = "Failed to connect to remote NuGet host";

                    if (times < 5)
                    {
                        message += "try again in 5 seconds";
                    }
                    
                    mLogger.Debug(message);
                    
                }

                catch (Exception ex)
                {
                    times++;

                    mLogger.Error("Failed to download package due to unknown error, tring again in 5 seconds",ex);

                    Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                }
            }

            if (times == 5)
            {
                mLogger.Error("Failed to download package: " + package.ID);
            }

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

        // Todo: Implement IDisposable that return task????
        public async Task Dispose()
        {
            mPackageManager.PackageInstalled -= DownloadDependencies;

            await CopyFilesToLocalRepository();

            Directory.Delete(mPackageManager.LocalRepository.Source, true);
        }
    }

}
