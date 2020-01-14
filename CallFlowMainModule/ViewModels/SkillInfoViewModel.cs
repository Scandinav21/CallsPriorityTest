using CallFlowCore.Messages;
using CallFlowCore.Services;
using CallFlowModel;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CallFlowModules.ViewModels
{
    public class SkillInfoViewModel : BindableBase, INavigationAware
    {
        private Skill skill;
        public Skill CurrentSkill
        {
            get { return skill; }
            set { SetProperty(ref skill, value); }
        }

        private int operatorsCountInSKill;
        public int OperatorsCountInSkill
        {
            get { return operatorsCountInSKill; }
            set { SetProperty(ref operatorsCountInSKill, value); }
        }

        private int operGenStartIndex;
        public int OperatorsCountStartIndex
        {
            get { return operGenStartIndex; }
            set { SetProperty(ref operGenStartIndex, value); }
        }

        private string callsCount;
        public string CallsCount
        {
            get { return callsCount; }
            set { SetProperty(ref callsCount, value); }
        }

        private Dictionary<int,int> callsDurationAllocation;
        public Dictionary<int,int> CallsDurationAllocation
        {
            get 
            {
                CallsCount = "Общее количество звонков = " + callsDurationAllocation.Values.Select(c => c).Sum();
                return callsDurationAllocation;
            }
            set 
            { SetProperty(ref callsDurationAllocation, value); }
        }

        private int minCallDuration;
        public int MinCallDuration
        {
            get { return minCallDuration; }
            set { SetProperty(ref minCallDuration, value); }
        }

        private int maxCallDuration;
        public int MaxCallDuration
        {
            get { return maxCallDuration; }
            set { SetProperty(ref maxCallDuration, value); }
        }

        private string addSkillToListBtnContent;
        public string AddSkillToListBtnContent
        {
            get { return addSkillToListBtnContent; }
            set { SetProperty(ref addSkillToListBtnContent, value); }
        }

        public DelegateCommand AddSkillToList { get; set; }

        IEventAggregator eventAggregator;
        ISkillServices skillServices;

        public SkillInfoViewModel(IEventAggregator ea, ISkillServices ss)
        {
            AddSkillToList = new DelegateCommand(AddSkillToListExecute);

            eventAggregator = ea;
            skillServices = ss;
        }

        private void AddSkillToListExecute()
        {
            if(skill == null || skill.SkillName == "" || skill.Priority < 1 || operatorsCountInSKill < 1 || operGenStartIndex < 0
                || skill.CallsAllocationInterval < 1 || minCallDuration < 0 || maxCallDuration < 1 || maxCallDuration <= minCallDuration)
            {
                MessageBox.Show("Проверьте корректность данных", "Ошибка ввода данных!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CurrentSkill.Operators = skillServices.GenerateOperators(OperatorsCountInSkill, OperatorsCountStartIndex);
            CurrentSkill.CallAllocation = skillServices.GenerateCallsAllocation(CallsDurationAllocation, CurrentSkill.CallsAllocationInterval, MinCallDuration, MaxCallDuration);
            CurrentSkill.CallsDurationAllocation = CallsDurationAllocation;
            CurrentSkill.MinTalkTimeDur = MinCallDuration;
            CurrentSkill.MaxTalkTimeDur = MaxCallDuration;

            eventAggregator.GetEvent<NewSkillMessage>().Publish(CurrentSkill);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var param = navigationContext.Parameters["NewSkill"];

            if (param != null && (bool)param)
            {
                CurrentSkill = new Skill();
                CallsDurationAllocation = CurrentSkill.CallsDurationAllocation;
                MinCallDuration = CurrentSkill.MinTalkTimeDur;
                MaxCallDuration = CurrentSkill.MaxTalkTimeDur;
                AddSkillToListBtnContent = "Добавить скилл в список";
            }

            param = navigationContext.Parameters["EditSkill"];
            if (param != null)
            {
                CurrentSkill = (Skill)param;
                CallsDurationAllocation = CurrentSkill.CallsDurationAllocation;
                MinCallDuration = CurrentSkill.MinTalkTimeDur;
                MaxCallDuration = CurrentSkill.MaxTalkTimeDur;
                AddSkillToListBtnContent = "Сохранить изменения";
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }
    }
}
