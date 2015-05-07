using System.Collections.Generic;
using System.Threading.Tasks;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;

namespace Ishimotto.NuGet
{
    public interface IDependenciesContainer
    {
        /// <summary>
        /// Dependencies repository consist of all downloaded packages
        /// </summary>
        IDependenciesRepostory DependenciesRepostory { get; }

        Task<IEnumerable<PackageDto>> GetDependenciesAsync(PackageDto packageDto, bool updateRepository = true);
        Task<IEnumerable<PackageDto>> GetDependenciesAsync(string packageID, bool updateRepository = true);
        Task AddDependencies(IEnumerable<PackageDto> dtos);
    }
}