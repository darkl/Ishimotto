using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.NuGet.Ui.Annotations;

namespace Ishimotto.NuGet.Ui
{
    public class DownloadInfoModel : INotifyPropertyChanged, INuGetSettings
    {

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

        public IEnumerable<string> PackagesIds { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public DownloadInfoModel()
        {
            Prerelase = Enumerable.Empty<string>();
            PackagesIds = Enumerable.Empty<string>();
        }
    }
}
