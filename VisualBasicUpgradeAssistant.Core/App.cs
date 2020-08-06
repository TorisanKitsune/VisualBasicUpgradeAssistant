using MvvmCross.IoC;
using MvvmCross.ViewModels;
using VisualBasicUpgradeAssistant.Core.ViewModels;

namespace VisualBasicUpgradeAssistant.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            //Mvx.IoCProvider.RegisterType<OmegaViewModel>();
            CreatableTypes()
                .EndingWith("ViewModel")
                .AsTypes()
                .RegisterAsDynamic();

            RegisterAppStart<MainViewModel>();
        }
    }
}
