using System;
using System.Collections.Generic;
using System.Linq;
using SharpConfig;

namespace Ishimotto.NuGet
{
    public interface INuGetSettings
    {
        string NuGetUrl { get; set; }

        /// <summary>
        /// Directory to direct the downloads to
        /// </summary>
        string DownloadDirectory { get; set; }

        /// <summary>
        /// The url of NuGet Gallery
        /// </summary>
        string RemoteRepositoryUrl { get; set; }

        /// <summary>
        /// The type of the Repository to savwe all the dependencies
        /// </summary>
        Type DependenciesRepositoryType { get; set; }

        /// <summary>
        /// Gets or sets the list of packages that must be downloaded, even if the lastest version is Prerelease
        /// </summary>
        IEnumerable<string> Prerelase { get; set; }

         IEnumerable<string> PackagesIds { get;}
    }

    /// <summary>
    /// Essential settings to download NuGet packages
    /// </summary>
    public class NuGetSettings : INuGetSettings
    {
        #region Constants
        private const string NUGET_URL = "NuGetUrl";

        private const string DOWNLOAD_DIRECTORY = "DownloadDirectory";

        private const string REMOTE_REPOSITORY_URL = "RemoteRepositoryUrl";

        private const string DEPENDENCIES_REPOSITORY_TYPE = "DependenciesRepositoryType";

        private const string ALLOW_PRERELEASE = "AllowPreRealse"; 
        #endregion
        
        #region Constructor

        /// <summary>
        /// Creates new instance of <see cref="NuGetSettings"/>
        /// </summary>
        /// <param name="settings">The section in the configuration containing the nuGet settings</param>
        public NuGetSettings(Section settings)
        {
            NuGetUrl = settings[NUGET_URL].Value;

            DownloadDirectory = settings[DOWNLOAD_DIRECTORY].Value;

            RemoteRepositoryUrl = settings[REMOTE_REPOSITORY_URL].Value;

            DependenciesRepositoryType = Type.GetType(settings[DEPENDENCIES_REPOSITORY_TYPE].Value);

            Prerelase = GetPrerelaseIds(settings);

        } 

        #endregion

        #region Private Methods
        /// <summary>
        /// Gets the prerelease packages to download from the configuration
        /// </summary>
        /// <param name="settings">The section in the configuration containing the nuGet settings</param>
        /// <returns>The ids of the prerelease packages to download</returns>
        private static IEnumerable<string> GetPrerelaseIds(Section settings)
        {
            var ids = settings[ALLOW_PRERELEASE].Value;

            if (ids.Contains(","))
            {
                return ids.Split(',').Where(id => String.IsNullOrEmpty(id));
            }
            else
            {
                return new[] { ids };
            }
        } 
        #endregion

        #region Properties
        public string NuGetUrl { get; set; }

        /// <summary>
        /// Directory to direct the downloads to
        /// </summary>
        public string DownloadDirectory { get; set; }

        /// <summary>
        /// The url of NuGet Gallery
        /// </summary>
        public string RemoteRepositoryUrl { get; set; }

        /// <summary>
        /// The type of the Repository to savwe all the dependencies
        /// </summary>
        public Type DependenciesRepositoryType { get; set; }

        /// <summary>
        /// Gets or sets the list of packages that must be downloaded, even if the lastest version is Prerelease
        /// </summary>
        public IEnumerable<string> Prerelase { get; set; }

        public IEnumerable<string> PackagesIds
        {
            get { return Enumerable.Empty<string>(); }
            
        }

        #endregion
        
    }

}