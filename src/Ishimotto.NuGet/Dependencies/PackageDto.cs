using System;
using MongoDB.Bson.Serialization.Attributes;
using NuGet;

namespace Ishimotto.NuGet.Dependencies
{
    /// <summary>
    /// Represent a reduced version of <see cref="IPackage"/> with the essential properties only
    /// </summary>
    public class PackageDto : IEquatable<PackageDto>
    {
        private string mMongoId;

        #region Properties

        /// <summary>
        /// The Id of the package
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The version of the Package
        /// </summary>
        [BsonIgnore]
        public SemanticVersion SemanticVersion { get; set; }

        public string Version
        {
            get { return SemanticVersion.ToString(); }


            set { SemanticVersion = SemanticVersion.Parse(value); }
        }

        [BsonId]
        public string MongoID
        {
            get { return FormatPackageID(); }
            set { mMongoId = value; }
        }


        public DateTime InsertionTime { get; private set; }
        
        #endregion

        #region Constructors
        /// <summary>
        /// Creates new instance of <see cref="PackageDto"/> 
        /// </summary>
        /// <param name="package">package to extract info from</param>
        internal PackageDto(IPackageName package)
            : this(package.Id, package.Version)
        {

        }

        public PackageDto()
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
        /// <param name="semanticVersion">The version of the package</param>
        internal PackageDto(string id, SemanticVersion semanticVersion)
            
        {
            ID = id;
            SemanticVersion = semanticVersion;
            InsertionTime = DateTime.Today;
        }

        #endregion

        /// <summary>
        /// Formats the package to a string format
        /// </summary>
        /// <returns><see cref="String"/> reprenting download link for browsers</returns>
        public string GetDownloadLink()
        {
            return String.Format("{0}/{1}/{2}", "http://nuget.org/api/v2/package/", ID,
                SemanticVersion);
        }

        /// <summary>
        /// Gets a fotmatted id to use as the repository unique key
        /// </summary>
        /// <returns>formatted id in the format: {ID}.{Version}</returns>
        public string FormatPackageID()
        {
            return String.Format("{0}.{1}", ID, Version);
        }

        #region IEquatable Implemntation
        /// <summary>
        /// Determines whether 2 <see cref="PackageDto"/> are equals
        /// </summary>
        /// <param name="other">Package to compare with the current package</param>
        /// <returns></returns>
        public bool Equals(PackageDto other)
        {
            return ID == other.ID && SemanticVersion.Equals(other.SemanticVersion);
        }

        #endregion
    }
}
