using CallFlowModules.Views;
using CallFlowModules;
using Prism.Ioc;
using Prism.Modularity;
using CallFlowCore.Services;

namespace CallFlowModules
{
    public class CallFlowMainModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<SkillInfo>();
            containerRegistry.RegisterForNavigation<EmptyView>();

            
        }
    }
}