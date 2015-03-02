using Ishimotto.NuGet.NuGetGallery;
using log4net;
using NuGet;

namespace Ishimotto.NuGet.Dependencies
{

    /// <summary>
    /// Simple extensions to build a <see cref="PackageDto"/> from diffrent entities
    /// </summary>
    public static class PackageExtentions
    {

        private static ILog logger = LogManager.GetLogger(typeof(PackageExtensions));


        /// <summary>
        /// Creates new <see cref="PackageDto"/> from <see cref="PackageDependency"/>
        /// </summary>
        /// <param name="package">Package to build <see cref="PackageDto"/> with</param>
        /// <returns><see cref="PackageDto"/> form of <see cref="package"/></returns>
        public static PackageDto ToDto(this PackageDependency package, IPackageRepository nugetRepository)
        {

            /* This is a defence mechanism against stupidity:
             * Stupid people refernece their packages to packages that does not exist yet.
             * So for every dependency I ask the source repository to bring a suitable package */

            IPackage packageFromSourceRepository;

            if (package.VersionSpec == null)
            {
                packageFromSourceRepository = nugetRepository.FindPackage(package.Id);
            }

            else
            {
                packageFromSourceRepository = nugetRepository.FindPackage(package.Id,
              package.VersionSpec, false, false);
                if (packageFromSourceRepository == null)
                {

              /* If we couldn't fins a stable version of the dependency, we will search for a pre release
               * I Don't understand how something stable depnds on pre release ...
               * Moreover, I agree to download dependency that are not listed because it necessary to the funcuality of a package
               */
                    packageFromSourceRepository = nugetRepository.FindPackage(package.Id,
                        package.VersionSpec, true, true);
                }

            }
          
                if (packageFromSourceRepository == null)
                {
                    logger.ErrorFormat(
                        "Failed to find sutable version of package {0}, the package won't be downloaded", package.Id);
                    
                    return null;
                }
            
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

        public static PackageDto ToDto(this V2FeedPackage package)
        {
            return new PackageDto(package.Id, package.Version);
        }
    }
}
