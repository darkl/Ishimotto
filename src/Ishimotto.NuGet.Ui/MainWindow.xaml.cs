using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ishimotto.NuGet.Ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public IshimottoViewModel ViewModel { get;  set; }

        public MainWindow()
        {

            var info = GetDownloadInformation();

            ViewModel = new IshimottoViewModel(info);

            DataContext = ViewModel;
            InitializeComponent();
        }

        private static DownloadInfoModel GetDownloadInformation()
        {
            var nugetUrl = ConfigurationManager.AppSettings["NuGetUrl"];

            var downloadDirectory = ConfigurationManager.AppSettings["DownloadDirectory"];

            var remoteUrl = ConfigurationManager.AppSettings["RemoteRepositoryUrl"];

            var info = new DownloadInfoModel()
            {
                DownloadDirectory = downloadDirectory,
                NuGetUrl = nugetUrl,
                RemoteRepositoryUrl = remoteUrl
            };
            return info;
        }
    }
}
