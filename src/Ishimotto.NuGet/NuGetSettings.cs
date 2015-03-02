﻿using System;
using SharpConfig;

namespace Ishimotto.NuGet
{
    /// <summary>
    /// Essentail settings to download NuGet packages
    /// </summary>
    public class NuGetSettings
    {

        private const string NUGET_URL = "NuGetUrl";

        private const string DOWNLOAD_DIRECTORY = "DownloadDirectory";

        private const string REMOTE_REPOSITORY_URL = "RemoteRepositoryUrl";

        private const string DEPENDENCIES_REPOSITORY_TYPE = "DependenciesRepositoryType";

        

        public NuGetSettings(Section settings)
        {
            NuGetUrl = settings[NUGET_URL].Value;

            DownloadDirectory = settings[DOWNLOAD_DIRECTORY].Value;

            RemoteRepositoryUrl = settings[REMOTE_REPOSITORY_URL].Value;

            DependenciesRepositoryType = Type.GetType(settings[DEPENDENCIES_REPOSITORY_TYPE].Value);
        }

        public string NuGetUrl { get; set; }

        public string DownloadDirectory { get; set; }

        public string RemoteRepositoryUrl { get; set; }

        public Type DependenciesRepositoryType { get; set; }
        
    }

}