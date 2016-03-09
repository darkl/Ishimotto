using System.Collections.Generic;
using System.Threading.Tasks;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using NuGet;

namespace Ishimotto.NuGet
{
    public class EmptyRepository : IDependenciesRepostory
    {
        public bool IsExist(PackageDto dependency)
        {
            return false;
        }

        public async Task AddDepndenciesAsync(IEnumerable<PackageDto> dependencies)
        {
        }

        public async Task AddDependnecyAsync(PackageDto package)
        {
            return;
        }

        public bool ShouldDownload(PackageDependency dependency)
        {
            return true;
        }
    }
}