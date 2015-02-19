using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Ishimotto.NuGet.NuGetDependencies;
using Ishimotto.NuGet.NuGetGallery;
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
        private INuGetDependenciesRepostory mDependenciesRepostory;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of <see cref="PackageManagerDownloader"/>
        /// </summary>
        /// <param name="nugetRepository">path to the source NuGet repository (NuGet website)</param>
        /// <param name="localRepository">path to the destenation repository</param>
        /// <param name="dependenciesRepostory">Entity to check if package's depndencies are needed</param>
        public PackageManagerDownloader(string nugetRepository, string localRepository, INuGetDependenciesRepostory dependenciesRepostory = null)
        {
            mPackageManager = new PackageManager(PackageRepositoryFactory.Default.CreateRepository(nugetRepository), localRepository);

            mPackageManager.LocalRepository.PackageSaveMode = PackageSaveModes.Nupkg;

            mPackageManager.PackageInstalled += DownloadDependencies;

            mDependenciesRepostory = dependenciesRepostory;
        }

        private async void DownloadDependencies(object sender, PackageOperationEventArgs e)
        {
            var dependnciesToDownload = from set in e.Package.DependencySets
                                        from depndency in set.Dependencies
                                        where mDependenciesRepostory.IsExist(depndency)
                                        select depndency.ToDto();

            await mDependenciesRepostory.AddDepndencies(
                    dependnciesToDownload.Concat(Enumerable.Repeat(e.Package.ToDto(), 1)));

            await DownloadPackagesAsync(dependnciesToDownload);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Download packages including their dependencies
        /// </summary>
        /// <param name="packages">Packages to download</param>
        public Task DownloadPackagesAsync(IEnumerable<PackageDto> packages)
        {
            return Task.Run(() =>
            {
                foreach (var package in packages)
                {
                    mPackageManager.InstallPackage(package.ID, package.Version, true, false);
                }
            });
        }

        /// <summary>
        /// Download packages from a specific time
        /// </summary>
        /// <param name="fetchFrom">The time to download packages from</param>
        public async Task DownloadPackagesAsync(DateTime fetchFrom)
        {
            var packages = from package in mPackageManager.SourceRepository.GetPackages()
                where package.IsLatestVersion &&
                      package.Listed && package.Published.HasValue &&
                      package.Published.Value.DateTime.CompareTo(fetchFrom) >= 0
                select package.ToDto();

            await DownloadPackagesAsync(packages);

        }

        #endregion

    }

}
