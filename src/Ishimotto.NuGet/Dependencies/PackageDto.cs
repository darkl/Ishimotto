using System;
using NuGet;

namespace Ishimotto.NuGet.Dependencies
{
    /// <summary>
    /// Represent a reduced version of <see cref="IPackage"/> with the essential properties only
    /// </summary>
    public class PackageDto : IEquatable<PackageDto>
    {
        #region Properties

        /// <summary>
        /// The Id of the package
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// The version of the Package
        /// </summary>
        public SemanticVersion Version { get; private set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Creates new instance of <see cref="PackageDto"/> 
        /// </summary>
        /// <param name="package">package to extract info from</param>
        internal PackageDto(PackageDependency package)
            : this(package.Id,package.VersionSpec.MaxVersion)
        {
            
        }

        /// <summary>
        /// Creates new instance of <see cref="PackageDto"/> 
        /// </summary>
        /// <param name="package">package to extract info from</param>
        internal PackageDto(IPackageName package)
            : this(package.Id, package.Version)
        {

        }

        /// <summary>
        /// Creates new instance of <see cref="PackageDto"/> 
        /// </summary>
        /// <param name="id">The Id of the package</param>
        /// <param name="version">The version of the package</param>
        public PackageDto(string id, string version)
            : this(id, SemanticVersion.Parse(version))
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="PackageDto"/> 
        /// </summary>
        /// <param name="id">The Id of the package</param>
        /// <param name="version">The version of the package</param>
        internal PackageDto(string id, SemanticVersion version)
            
        {
            ID = id;
            Version = version;
        }

        #endregion

        #region IEquatable Implemntation
        /// <summary>
        /// Determines whether 2 <see cref="PackageDto"/> are equals
        /// </summary>
        /// <param name="other">Package to compare with the current package</param>
        /// <returns></returns>
        public bool Equals(PackageDto other)
        {
            return ID == other.ID && Version.Equals(other.Version);
        }

        #endregion
    }
}
