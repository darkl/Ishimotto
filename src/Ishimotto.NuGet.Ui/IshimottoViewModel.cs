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
    public class IshimottoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        private DateTime _fetchingDate;

        public DateTime FetchingDate
        {
            get { return FetchingDate; }
            set
            {
                FetchingDate = value;
                OnPropertyChanged();
            }
        }

        private string _packagesIds;

        public string PackagesIds
        {
            get { return _packagesIds; }
            set
            {
                _packagesIds = value;
                OnPropertyChanged();
            }
        }

        private bool _includePreRelease;

        public bool IncludePreRelease
        {
            get { return _includePreRelease; }
            set
            {
                _includePreRelease = value;
                OnPropertyChanged();
            }
        }


        public DateTime MaxFetchingDate
        {
            get { return DateTime.Now; }
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
