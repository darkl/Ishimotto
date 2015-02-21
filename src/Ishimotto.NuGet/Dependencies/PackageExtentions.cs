using NuGet;

namespace Ishimotto.NuGet.Dependencies
{

    /// <summary>
    /// Simple extensions to build a <see cref="PackageDto"/> from diffrent entities
    /// </summary>
    public static class PackageExtentions
    {
        /// <summary>
        /// Creates new <see cref="PackageDto"/> from <see cref="PackageDependency"/>
        /// </summary>
        /// <param name="package">Package to build <see cref="PackageDto"/> with</param>
        /// <returns><see cref="PackageDto"/> form of <see cref="package"/></returns>
        public static PackageDto ToDto(this PackageDependency package, IPackageManager packageManager)
        {

            /* This is a defence mechanism against stupidity:
             * Stupid people refernece their packages to packages that does not exist yet.
             * So for every dependency I ask the source repository to bring a suitable package */

            var packageFromSourceRepository = packageManager.SourceRepository.FindPackage(package.Id,
                package.VersionSpec, false, false);
            
            return new PackageDto(packageFromSourceRepository);
        }

        /// <summary>
        /// Creates new <see cref="PackageDto"/> from <see cref="IPackageName"/>
        /// </summary>
        /// <param name="package">Package to build <see cref="PackageDto"/> with</param>
        /// <returns><see cref="PackageDto"/> form of <see cref="package"/></returns>
        public static PackageDto ToDto(this IPackageName package)
        {
            return new PackageDto(package);
        }
    }
}
