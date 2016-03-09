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
    public class DependenciesContainer : IDependenciesContainer
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
        /// Creates new instance of <see cref="DependenciesContainer"/>
        /// </summary>
        /// <param name="nugetRepository">path to the source NuGet repository (NuGet website)</param>
        /// <param name="localRepository">path to the destenation repository</param>
        /// <param name="dependenciesRepostoryInfo">Entity to check if package's depndencies are needed</param>
        public DependenciesContainer(string nugetRepository, IDependenciesRepostoryInfo dependenciesRepostoryInfo)
        {
            mLogger = LogManager.GetLogger(typeof(DependenciesContainer).Name);

            mNugetRepository = PackageRepositoryFactory.Default.CreateRepository(nugetRepository);


            DependenciesRepostory = dependenciesRepostoryInfo.Build();
        }

        protected DependenciesContainer(string remoteRepositoryUrl) :this(remoteRepositoryUrl,new NoRepositoryInfo())
        {
           
        }

        #endregion

        #region Public Methods

        public virtual  Task<IEnumerable<PackageDto>> GetDependenciesAsync(PackageDto packageDto, bool updateRepository = true)
        {
            return  GetDependenciesAsync(packageDto.ID, updateRepository);
        }

        

        public virtual async Task<IEnumerable<PackageDto>> GetDependenciesAsync(string packageID, bool updateRepository = true)
        {
            //TODO: Support diffrent kinds of frameworks

            mLogger.InfoFormat("Downloading dependencies of {0}", packageID);

            var package =
                mNugetRepository.FindPackage(packageID);

            var dependencies =
                from depndency in
                    GetCompetiblePackagesForAllFrameworks(package) 
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

                await DependenciesRepostory.AddDepndenciesAsync(validDependencies).ConfigureAwait(false);
            }

            return validDependencies;
        }

        private static IEnumerable<PackageDependency> GetCompetiblePackagesForAllFrameworks(IPackage package)
        {
            List<PackageDependency> depenencies = new List<PackageDependency>();

            var targetFrameworks = package.DependencySets.Select(set => set.TargetFramework);

            foreach (var framework in targetFrameworks)
            {
                depenencies.AddRange(package.GetCompatiblePackageDependencies(framework));
            }

            return depenencies.Distinct();
        }

        private static string FormatPackages(IEnumerable<PackageDto> validDependencies)
        {
            return String.Join("," + Environment.NewLine, validDependencies.Select(dependency => dependency.ID));
        }

        #endregion

        public virtual Task AddDependencies(IEnumerable<PackageDto> dtos)
        {
            mLogger.InfoFormat("Add packages to the repository");

            return DependenciesRepostory.AddDepndenciesAsync(dtos);
        }
    }
}
