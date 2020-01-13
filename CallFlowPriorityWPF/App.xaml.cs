using CallFlowModules.Views;
using CallFlowModules;
using CallFlowPriorityWPF.Views;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;
using CallFlowCore.Services;

namespace CallFlowPriorityWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ISkillServices, SkillServices>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<CallFlowMainModule>();
        }
    }
}
