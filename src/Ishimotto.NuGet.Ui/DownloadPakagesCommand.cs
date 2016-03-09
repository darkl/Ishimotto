using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ishimotto.NuGet.Ui
{
    class DownloadPakagesCommand : ICommand
    {

        private IshimottoViewModel _viewModel;

        public DownloadPakagesCommand(IshimottoViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter)
        {
            var fetchFrom = (DateTime)parameter;
            
            return fetchFrom.CompareTo(default(DateTime)) == 0;
        }

        public async void Execute(object parameter)
        {
            var nugetTask = new NuGetDownloadAsyncTask(_viewModel.DownloadInfoViewModel, _viewModel.DC);

            await nugetTask.ExecuteAsync();
        }

        public event EventHandler CanExecuteChanged;
    }
}
