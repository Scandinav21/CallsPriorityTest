using CallFlowCore.Converters;
using CallFlowCore.Messages;
using CallFlowCore.Services;
using CallFlowModel;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CallFlowModules.ViewModels
{
    public class MainModuleViewModel : BindableBase
    {
        IRegionManager regionManager;

        private ObservableCollection<Skill> skills;
        public ObservableCollection<Skill> Skills
        {
            get { return skills; }
            set { SetProperty(ref skills, value); }
        }

        private Skill selectedSkill;
        public Skill SelectedSkill
        {
            get 
            {
                if (selectedSkill != null)
                    AddSkillBtnContent = "Изменить выбранный скилл";
                else
                    AddSkillBtnContent = "Добавить скилл";

                return selectedSkill; 
            }
            set 
            { 
                SetProperty(ref selectedSkill, value);
            }
        }

        public DelegateCommand AddSkill { get; set; }
        public DelegateCommand StartSimulation { get; set; }
        public DelegateCommand StopSimulation { get; set; }

        private int currentTime;
        public int CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }

        private string mainStatisticsInfo;
        public string MainStatisticsInfo
        {
            get { return mainStatisticsInfo; }
            set { SetProperty(ref mainStatisticsInfo, value); }
        }

        private int simulationSpeed;
        public int SimulationSpeed
        {
            get { return simulationSpeed; }
            set { SetProperty(ref simulationSpeed, value); }
        }

        private string addSkillBtnContent;
        public string AddSkillBtnContent
        {
            get { return addSkillBtnContent; }
            set { SetProperty(ref addSkillBtnContent, value); }
        }

        ISkillServices skillServices;

        CancellationTokenSource cancellationToken;

        public MainModuleViewModel(IRegionManager rm, IEventAggregator ea, ISkillServices skillServ)
        {
            regionManager = rm;
            skillServices = skillServ;

            AddSkill = new DelegateCommand(AddSkillExecute);
            StartSimulation = new DelegateCommand(StartSimulationExecute);
            StopSimulation = new DelegateCommand(StopSimulationExecute);

            AddSkillBtnContent = "Создать скилл";

            ea.GetEvent<NewSkillMessage>().Subscribe(GetNewSkill);

            Skills = new ObservableCollection<Skill>();
            CurrentTime = 0;
            SimulationSpeed = 1000;
        }

        #region Commands

        private void StopSimulationExecute()
        {
            cancellationToken.Cancel();
        }

        private async void StartSimulationExecute()
        {
            cancellationToken = new CancellationTokenSource();
            await Task.Run(() => SimulationProcess(), cancellationToken.Token);
        }

        public void AddSkillExecute()
        {
            var navParams = new NavigationParameters();

            if (SelectedSkill != null)
            {
                navParams.Add("EditSkill", SelectedSkill);
            }
            else
                navParams.Add("NewSkill", true);

            AddSkillBtnContent = "";

            regionManager.RequestNavigate("SkillInfoRegion", "SkillInfo", navParams);
        }

        #endregion


        private async void SimulationProcess()
        {
            const int MaxPeriodTime = 86400;
            int periodSec = Skills[0].CallsAllocationInterval;

            CurrentTime = 0;

            int periodRequestData = periodSec;

            Skills = ConvertObservableCollection.ToObservableCollection(Skills.OrderBy(s => s.Priority).ToList());

            while(!cancellationToken.IsCancellationRequested)
            {
                foreach (var skill in Skills)
                {
                    //Обновляем время в операторах и скиллах
                    skill.statistic.AbandonedCalls += skillServices.UpdateSkillData(Skills.ToList(), skill);

                    //if (skill.SkillName == "SG_21")
                    //    TryRaisePriority(skill, 60, 2);

                    skillServices.CheckQueueInSkills(Skills.ToList(), CurrentTime);

                    foreach (var call in skill.CallAllocation.Item1.Where(c => c == CurrentTime - (periodRequestData - periodSec)).ToList())
                    {
                        skill.statistic.CallsOffered++;

                        //Ищем свободного оператора
                        Operator oper = skillServices.GetFreeOperator(skill.Operators);

                        //Добавляем новый звонок в очередь на скилле
                        if (oper == null)
                            skillServices.PutCallToQueue(Skills.ToList(), skill, CurrentTime - (periodRequestData - periodSec));
                        else
                            //Добавляем звонок оператору
                            skillServices.AnswerCall(oper, Skills.ToList(), skill, CurrentTime - (periodRequestData - periodSec));
                    }
                }

                MainStatisticsInfo = skillServices.GetStatistics(CurrentTime, Skills.ToList());
                await Task.Delay(SimulationSpeed);
                CurrentTime++;
            }

            Skills = skillServices.ResetSkills(Skills);
        }

        private void GetNewSkill(Skill skill)
        {
            if (skill != null)
            {
                if (Skills.Select(s => s.SkillName).Where(sn => sn == skill.SkillName).Count() > 0)
                    Skills.Remove(skills.Select(s => s).Where(s => s.SkillName == skill.SkillName).FirstOrDefault());

                Skills.Add(skill);
            }

            AddSkillBtnContent = "Создать скилл";
            regionManager.RequestNavigate("SkillInfoRegion", "EmptyView");
        }
    }
}
