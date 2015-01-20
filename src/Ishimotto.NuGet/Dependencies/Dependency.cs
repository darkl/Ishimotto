using System;
using System.Linq;

namespace Ishimotto.NuGet.Dependencies
{
    /// <summary>
    /// Basic implemntation of <see cref="INuGetDependency"/>
    /// </summary>
    public class NuGetDependency : INuGetDependency, IEquatable<NuGetDependency>
    {
        #region Constructors
        /// <summary>
        /// Creates new instance of <see cref="NuGetDependency"/>
        /// </summary>
        /// <param name="stringDependency">encoded string in the format {<see cref="PackageId"/>:<see cref="Version"/>}</param>
        public NuGetDependency(string stringDependency)
        {
            var args = stringDependency.Split(':');

            if (args.Length < 1)
            {
                throw new ArgumentException("Parameter string NuGetDependency is not format well, format should be [PackageID:PackageVersion]");
            }

            PackageId = args[0];

            if (args.Length > 1)
            {
                Version = args[1];
            }
            else
            {
                Version = string.Empty;
            }
        }

        /// <summary>
        /// Creates new instance of <see cref="NuGetDependency"/>
        /// </summary>
        /// <param name="packageId"><see cref="PackageId"/></param>
        /// <param name="version"><see cref="Version"/></param>
        public NuGetDependency(string packageId, string version)
        {
            PackageId = packageId;
            Version = version;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the package id of the dependency
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the version of the dependency
        /// </summary>
        public string Version { get; private set; }
        #endregion

        #region Overrides
        /// <summary>
        /// Checks if 2 <see cref="NuGetDependency"/>'s are equal
        /// </summary>
        /// <param name="other"><see cref="NuGetDependency"/></param>
        /// <returns><see cref="bool"/> indicating if the elements are equals</returns>
        public bool Equals(NuGetDependency other)
        {
            return other.PackageId.Equals(PackageId) && other.PackageId.Equals(PackageId);
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:", PackageId, Version);
        }
        #endregion
    }

    /// <summary>
    /// Represent a NuGet dependency
    /// </summary>
    public interface INuGetDependency
    {
        /// <summary>
        /// Gets the package id of the dependency
        /// </summary>
        string PackageId { get; }

        /// <summary>
        /// Gets the version of the dependency
        /// </summary>
        string Version { get; }
    }
}
