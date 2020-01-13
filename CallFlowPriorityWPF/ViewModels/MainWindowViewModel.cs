using CallFlowModules.Views;
using Prism.Mvvm;
using Prism.Regions;

namespace CallFlowPriorityWPF.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Call flow simulator";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel(IRegionManager regionManager)
        {
            regionManager.RegisterViewWithRegion("MainRegion", typeof(MainModule));
        }
    }
}
