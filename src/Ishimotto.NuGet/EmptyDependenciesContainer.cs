using System.Collections.Generic;
using System.Threading.Tasks;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;

namespace Ishimotto.NuGet
{
    internal class EmptyDependenciesContainer : DependenciesContainer
    {

        

        public EmptyDependenciesContainer(string remoteRepositoryUrl) : base(remoteRepositoryUrl)
        {
        }


        public override Task<IEnumerable<PackageDto>> GetDependenciesAsync(PackageDto packageDto, bool updateRepository = true)
        {
            return base.GetDependenciesAsync(packageDto, false);
        }

        public override Task<IEnumerable<PackageDto>> GetDependenciesAsync(string packageID, bool updateRepository = true)
        {
            return base.GetDependenciesAsync(packageID, false);
        }

        public override Task AddDependencies(IEnumerable<PackageDto> dtos)
        {
            return Task.Run(() => 6);
        }
    }
}