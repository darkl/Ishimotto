using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            
            var nugetTask = new NuGetDownloadAsyncTask(_viewModel.DownloadInfoModel,_viewModel.FetchingDate,(status) => _viewModel.Status = status);

            Task.Run(async() => await nugetTask.ExecuteAsync().ConfigureAwait(false)).ContinueWith(
                (task) =>
                {
                    _viewModel.IsDownloadCommandEnabled = true;

                    if (task.IsFaulted)
                    {
                        _viewModel.Status = "Error occured, please check the log";
                    }

                    else
                    {
                        _viewModel.Status = "Finsih downloading packages from " +
                                            _viewModel.FetchingDate.ToShortDateString();
                    }
                    
                }
                );
            
        }

        public event EventHandler CanExecuteChanged;

        
    }
}
