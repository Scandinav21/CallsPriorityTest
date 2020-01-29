using CallFlowCore.Converters;
using CallFlowCore.Messages;
using CallFlowCore.Services;
using CallFlowModel;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
                {
                    AddSkillBtnContent = "Изменить выбранный скилл";
                    DeleteSkill.RaiseCanExecuteChanged();
                }
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
        public DelegateCommand DeleteSkill { get; set; }
        public DelegateCommand SkillSelectedChanged { get; set; }
        public DelegateCommand OpenHelp { get; set; }

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

        private bool btnStartSimulationEnabled;
        public bool BtnStartSimulationEnabled
        {
            get { return btnStartSimulationEnabled; }
            set { SetProperty(ref btnStartSimulationEnabled, value); }
        }

        private int progressBarValue;
        public int ProgressBarValue
        {
            get { return progressBarValue; }
            set { SetProperty(ref progressBarValue, value); }
        }

        ISkillServices skillServices;

        CancellationTokenSource cancellationToken;

        public MainModuleViewModel(IRegionManager rm, IEventAggregator ea, ISkillServices skillServ)
        {
            regionManager = rm;
            skillServices = skillServ;

            AddSkill = new DelegateCommand(AddSkillExecute);
            StartSimulation = new DelegateCommand(StartSimulationExecute, CanStartSimulation);
            DeleteSkill = new DelegateCommand(DeleteSkillExecute, CanDeleteSkill);
            StopSimulation = new DelegateCommand(StopSimulationExecute);
            SkillSelectedChanged = new DelegateCommand(SkillSelectedChangedExecute);
            OpenHelp = new DelegateCommand(OpenHelpExecute);

            AddSkillBtnContent = "Создать скилл";

            ea.GetEvent<NewSkillMessage>().Subscribe(GetNewSkill);

            Skills = new ObservableCollection<Skill>();
            Skills.CollectionChanged += Skills_CollectionChanged;
            CurrentTime = 0;
            SimulationSpeed = 1000;

            BtnStartSimulationEnabled = true;

            DeserializeSkills();
        }

        public void DeserializeSkills()
        {
            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();

            using(StreamReader sr = new StreamReader("Skills.json"))
            using (JsonTextReader jsr = new JsonTextReader(sr))
            {
                object tempSkills = jsonSerializer.Deserialize(jsr, typeof(ObservableCollection<Skill>));

                if (tempSkills != null && tempSkills is ObservableCollection<Skill>)
                    Skills = (ObservableCollection<Skill>)tempSkills;
            }
        }

        ~MainModuleViewModel()
        {
            JsonSerializer jsr = JsonSerializer.CreateDefault();

            using (StreamWriter sr = new StreamWriter("Skills.json"))
            using (JsonTextWriter jsw = new JsonTextWriter(sr))
            {
                jsr.Serialize(jsw, Skills);
            }
        }

        private void Skills_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            StartSimulation.RaiseCanExecuteChanged();
            DeleteSkill.RaiseCanExecuteChanged();
        }

        #region Commands

        private void OpenHelpExecute()
        {
            Process.Start("Help.txt");
        }

        private void SkillSelectedChangedExecute()
        {
            if (cancellationToken != null && cancellationToken.Token.IsCancellationRequested && SelectedSkill != null)
                MainStatisticsInfo = skillServices.GetStatistics(CurrentTime, Skills.ToList(), SelectedSkill);
        }

        private bool CanDeleteSkill()
        {
            if (Skills != null && Skills.Count > 0 && selectedSkill != null)
                return true;
            else
                return false;
        }

        private void DeleteSkillExecute()
        {
            if (Skills != null && Skills.Count > 0)
            {
                Skills.Remove(SelectedSkill);
                regionManager.RequestNavigate("SkillInfoRegion", "EmptyView");
            }
        }

        private bool CanStartSimulation()
        {
            if (Skills != null && Skills.Count > 0)
                return true;
            else
                return false;
        }

        private void StopSimulationExecute()
        {
            AddSkillBtnContent = "Создать скилл";
            BtnStartSimulationEnabled = true;
            cancellationToken.Cancel();
        }

        private async void StartSimulationExecute()
        {
            AddSkillBtnContent = "";
            BtnStartSimulationEnabled = false;
            cancellationToken = new CancellationTokenSource();

            MainStatisticsInfo = "Подготавливаю данные, пожалуйста, подождите...";

            ProgressBarValue = 0;
            int progressStep = Skills.Count;

            for (int i = 0; i < Skills.Count; i++)
            {
                ProgressBarValue += progressStep;
                Skills[i] = await Task.Run(() => skillServices.ResetSkill(Skills[i]));
            }

            ProgressBarValue = 0;

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
            CurrentTime = 0;
            int intervalRunning = 1;

            Skills = ConvertObservableCollection.ToObservableCollection(Skills.OrderBy(s => s.Priority).ToList());

            while(!cancellationToken.IsCancellationRequested)
            {
                foreach (var skill in Skills)
                {
                    //Reload calls data
                    if (CurrentTime == skill.CallsAllocationInterval * intervalRunning)
                    {
                        MainStatisticsInfo = "Подготавливаю данные для следующего интервала, пожалуйста, подождите...";

                        ProgressBarValue = 0;
                        int progressStep = Skills.Count;

                        foreach (var s in Skills)
                        {
                            ProgressBarValue += progressStep;
                            s.CallAllocation = await Task.Run(() => skillServices.GenerateCallsAllocation(s.CallsDurationAllocation, CurrentTime, s.CallsAllocationInterval, s.MaxTalkTimeDur));
                        }
                        
                        intervalRunning++;
                        ProgressBarValue = 0;
                    }

                    //Обновляем время в операторах и скиллах
                    skill.statistic.AbandonedCalls += skillServices.UpdateSkillData(Skills.ToList(), skill);

                    bool raisedPriority = false;

                    //Если задана очередь, на каких-либо скиллах
                    if (skill.PriorCondition.queueMultiple || skill.PriorCondition.queueSingle)
                        //Пробуем изменить приоритет вызовов
                        raisedPriority = skillServices.TryRaiseSkillPriority(Skills, skill);
                        
                    //AND 1
                    //OR -1
                    if (skill.PriorCondition.timeWait && skill.PriorCondition.timeWaitVal > 0 && ((skill.PriorCondition.unitCondition == 1 && raisedPriority) || (skill.PriorCondition.unitCondition == -1 && !raisedPriority) || skill.PriorCondition.unitCondition == 0))
                        skillServices.TryRaiseCallPriority(skill, skill.PriorCondition.timeWaitVal, skill.PriorCondition.priorityWhenTimeW);
                    
                    skillServices.CheckQueueInSkills(Skills.ToList(), CurrentTime);

                    while (skill.CallAllocation.Item1.Where(c => c == CurrentTime).Count() > 0)
                    {
                        skill.statistic.CallsOffered++;

                        //Ищем свободного оператора
                        Operator oper = skillServices.GetFreeOperator(skill.Operators);

                        //Добавляем новый звонок в очередь на скилле
                        if (oper == null)
                            skillServices.PutCallToQueue(Skills.ToList(), skill, CurrentTime);
                        else
                            //Добавляем звонок оператору
                            skillServices.AnswerCall(oper, Skills.ToList(), skill, CurrentTime);
                    }
                }

                if (SelectedSkill != null)
                    MainStatisticsInfo = skillServices.GetStatistics(CurrentTime, Skills.ToList(), SelectedSkill, false, true);
                else
                    MainStatisticsInfo = skillServices.GetStatistics(CurrentTime, Skills.ToList(), null, true);

                Task delay = Task.Delay(SimulationSpeed);
                delay.Wait(SimulationSpeed);
                CurrentTime++;
            }
        }

        private void GetNewSkill(Skill skill)
        {
            if (skill != null && Skills != null)
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
