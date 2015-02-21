using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet;

namespace Ishimotto.NuGet.Dependencies.Repositories
{
    /// <summary>
    /// Represent a repository of all known NuGet dependencies
    /// </summary>
    public interface IDependenciesRepostory
    {
        /// <summary>
        /// Determines wwther a dependency exist in the repository
        /// </summary>
        /// <param name="dependency">The dependency to check</param>
        /// <returns>Boolean indicating if <see cref="dependency"/> exists in the repository</returns>
        bool IsExist(PackageDto dependency);

        /// <summary>
        /// Adds new depdendencies to the repository
        /// </summary>
        /// <param name="dependencies">The depdendnecies to the repository</param>
        /// <returns>A task to indicate when the process is done</returns>
        Task AddDepndenciesAsync(IEnumerable<PackageDto> dependencies);


        /// <summary>
        /// Adds single package to the repository
        /// </summary>
        /// <param name="package">item to add</param>
        /// <returns>Task to indicate when the process is completed</returns>
        Task AddDependnecyAsync(PackageDto package);

        /// <summary>
        /// Determines whether a depdendency should be download
        /// </summary>
        /// <param name="dependency">Dependency to examine</param>
        /// <returns>Boolean indicating if the <see cref="dependency"/></returns>
        bool ShouldDownload(PackageDependency dependency);
    }
}
