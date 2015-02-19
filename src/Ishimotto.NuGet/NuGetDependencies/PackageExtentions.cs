using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.NuGet.NuGetGallery;
using NuGet;

namespace Ishimotto.NuGet.NuGetDependencies
{
    public static class PackageExtentions
    {
        public static PackageDto ToDto(this PackageDependency package)
        {
            return new PackageDto(package);
        }

        public static PackageDto ToDto(this IPackageName package)
        {
            return new PackageDto(package);
        }
    }
}
