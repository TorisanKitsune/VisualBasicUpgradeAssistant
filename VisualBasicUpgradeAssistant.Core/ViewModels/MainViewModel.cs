using MinoriEditorShell.Services;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace VisualBasicUpgradeAssistant.Core.ViewModels
{
    public class MainViewModel : MvxNavigationViewModel
    {
        public MainViewModel(
            IMvxLogProvider logProvider,
            IMesWindow mesWindow,
            IMvxNavigationService navigationService
            ) : base(logProvider, navigationService)
        {
            mesWindow.DisplayName = "Visual Basic Upgrade Assistant (Alpha)";
        }

        public IMvxCommand OpenOmegaCommand => new MvxCommand(() => NavigationService.Navigate<OmegaViewModel>());
        public IMvxCommand OpenProjectConverter => new MvxCommand(() => NavigationService.Navigate<ProjectConverterViewModel>());
    }
}
