using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using NuGet;

namespace Ishimotto.NuGet.Ui
{
    public class DownloadPakagesCommand : ICommand
    {

        private IshimottoViewModel _viewModel;

        public DownloadPakagesCommand(IshimottoViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
            
        }

        public void Execute(object parameter)
        {

            _viewModel.IsDownloadCommandEnabled = false;
            
            var nugetTask = new NuGetDownloadAsyncTask(_viewModel.DownloadInfoModel,_viewModel.FetchingDate);

            nugetTask.ExecuteAsync().ContinueWith((t) => _viewModel.IsDownloadCommandEnabled = true);


        }

        public event EventHandler CanExecuteChanged;

        
    }
}
