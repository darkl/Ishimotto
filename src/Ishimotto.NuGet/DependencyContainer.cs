using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
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
    public class DependencyContainer
    {
        #region Data Members

        /// <summary>
        /// Dependencies repository to prevent download of unnecessary packages
        /// </summary>
        public IDependenciesRepostory DependenciesRepostory { get; internal set; }

        private ILog mLogger;

        private IPackageRepository mNugetRepository;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates new instance of <see cref="DependencyContainer"/>
        /// </summary>
        /// <param name="nugetRepository">path to the source NuGet repository (NuGet website)</param>
        /// <param name="localRepository">path to the destenation repository</param>
        /// <param name="dependenciesRepostoryInfo">Entity to check if package's depndencies are needed</param>
        public DependencyContainer(string nugetRepository, IDependenciesRepostoryInfo dependenciesRepostoryInfo)
        {
            mLogger = LogManager.GetLogger(typeof(DependencyContainer).Name);

            mNugetRepository = PackageRepositoryFactory.Default.CreateRepository(nugetRepository);

            DependenciesRepostory = dependenciesRepostoryInfo.Build();
        }

        #endregion

        #region Public Methods

        public async Task<IEnumerable<PackageDto>> GetDependenciesAsync(PackageDto packageDto, bool updateRepository = true)
        {
            return await GetDependenciesAsync(packageDto.ID, updateRepository);
        }

        public async Task<IEnumerable<PackageDto>> GetDependenciesAsync(string packageID, bool updateRepository = true)
        {
            //TODO: Support diffrent kinds of frameworks

            mLogger.InfoFormat("Downloading pdependencies of {0}", packageID);

            var package =
                mNugetRepository.FindPackage(packageID);

            var dependencies =
                from depndency in
                    package.GetCompatiblePackageDependencies(new FrameworkName(".NETFramework,Version=v4.5"))
                where DependenciesRepostory.ShouldDownload(depndency)
                select depndency.ToDto(mNugetRepository);

            var validDependencies = dependencies.Where(d => d != null);

            if (updateRepository)
            {
                if (mLogger.IsInfoEnabled)
                {
                    mLogger.InfoFormat("Found {0} dependencies to the package {1} : {2}", validDependencies.Count(),
                        packageID, FormatPackages(validDependencies));

                    mLogger.InfoFormat("Adding dependencies of {0} to the repository", package.Id);

                }

                await DependenciesRepostory.AddDepndenciesAsync(validDependencies);
            }

            return validDependencies;
        }

        private static string FormatPackages(IEnumerable<PackageDto> validDependencies)
        {
            return String.Join("," + Environment.NewLine, validDependencies.Select(dependency => dependency.ID));
        }

        #endregion

        public async Task AddDependencies(IEnumerable<PackageDto> dtos)
        {
            mLogger.InfoFormat("Add packages to the repository");

            await DependenciesRepostory.AddDepndenciesAsync(dtos);
        }
    }

}
