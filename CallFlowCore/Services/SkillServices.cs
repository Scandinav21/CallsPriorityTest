﻿using CallFlowModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CallFlowCore.Converters;
using System.Collections.ObjectModel;

namespace CallFlowCore.Services
{
    public class SkillServices : ISkillServices
    {
        public List<Operator> GenerateOperators(int countOfOperators, int startIndex)
        {
            List<Operator> operators = new List<Operator>();

            for (int i = startIndex; i < countOfOperators + startIndex; i++)
                operators.Add(new Operator($"Operator{i + 1}"));

            return operators;
        }

        public (List<int>, List<int>) GenerateCallsAllocation(Dictionary<int, int> callDurationAllocation, int intervalSeconds, int minTalkTimePer, int maxTalkTimePer)
        {
            List<int> callsAllocation = new List<int>();
            List<int> callsTalkTimeAllocation = new List<int>();

            Random randomSeconds = new Random();

            while (callDurationAllocation.Values.Select(v => v).Sum() > callsAllocation.Count())
            {
                //Генерим время поступления вызова
                int newCallOfferedTime = randomSeconds.Next(1, intervalSeconds);

                ////Если вызов на такой секунде уже присутствует, то пропускаем добавление данного вызова в список
                //if (callsAllocation.Contains(newCallOfferedTime))
                //    continue;

                //Генерим длительность вызова
                int newCallDuration = randomSeconds.Next(minTalkTimePer, maxTalkTimePer);

                //Получаем ключ словаря с распределением соответственно длительности звонка
                int numberAllocationKey = callDurationAllocation.Keys.Select(c => c)
                    .Where(c => (newCallDuration >= c && newCallDuration < c + 60)
                    || (newCallDuration >= c && c == callDurationAllocation.Keys.Last())).FirstOrDefault();

                //Получаем количество вызовов с данным диапазоном длительности
                int callsWithTheSameDurationInterval = callsTalkTimeAllocation.Select(c => c)
                    .Where(c => (c >= numberAllocationKey && c < numberAllocationKey + 60 && numberAllocationKey < 660)
                        || (c >= callDurationAllocation.Keys.Last() && numberAllocationKey == callDurationAllocation.Keys.Last())).Count();

                if (callDurationAllocation[numberAllocationKey] > callsWithTheSameDurationInterval)
                {
                    callsAllocation.Add(newCallOfferedTime);
                    callsTalkTimeAllocation.Add(newCallDuration);
                }
            }

            //callsTalkTimeAllocation.Sort();
            //foreach (var call in callsTalkTimeAllocation)
            //{
            //    Console.WriteLine(call);
            //}

            callsAllocation.Sort();

            return (callsAllocation, callsTalkTimeAllocation);
        }

        public void AnswerCall(Operator oper, List<Skill> skills, Skill currentSkill, int currentTime)
        {
            Random random = new Random();

            //Добавляем звонок оператору
            oper.call = new Call(random.Next(1, 9999), currentSkill.CallAllocation.Item2[currentSkill.CallAllocation.Item1.IndexOf(currentTime)], currentTime, currentSkill.Priority);

            oper.call.CallAnsweredTime = currentTime;

            //Меняем статусы оператора
            oper.CurrentStatus = "Talking";
            oper.TimeInCurrentStatus = 0;

            //Добавляем звонок в активные по скиллу
            currentSkill.ActiveCalls.Add(oper.call);

            currentSkill.statistic.CallAnswered++;

            currentSkill.statistic.SLCalls++;

            currentSkill.statistic.Calls.Add(oper.call);

            SetOperStatusInAllSkills(skills, oper);
        }

        public void PutCallToQueue(List<Skill> skills, Skill skill, int currentTime)
        {
            Call call = new Call(GetFreeCallID(skills), skill.CallAllocation.Item2[skill.CallAllocation.Item1.IndexOf(currentTime)], currentTime, skill.Priority);

            call.CallOfferedTime = currentTime;

            skill.CallsInQueue.Add(call);
        }

        public void CheckQueueInSkills(List<Skill> skills, int currentTime)
        {
            List<Call> allCalls = new List<Call>();

            //Получаем все звонки со всех скиллов
            skills.Select(s => s.CallsInQueue).ToList().ForEach(lc => lc.ForEach(c => allCalls.Add(c)));

            //Сортируем по уменьшению приоритета
            allCalls = allCalls.OrderBy(c => c.CallPriority).ToList();

            foreach (var call in allCalls)
            {
                List<Call> callsSamePriority = new List<Call>();

                //Получаем вызовы с одинаковым приоритетом
                var t = skills.Select(s => s)
                    .Select(s => s.CallsInQueue)
                    .Select(lc => lc.Select(c => c).Where(c => c.CallPriority == call.CallPriority));

                foreach (var lc in t)
                {
                    foreach (var c in lc)
                    {
                        callsSamePriority.Add(c);
                    }
                }

                callsSamePriority = callsSamePriority.OrderBy(c => c.Duration).ToList();

                foreach (var c in callsSamePriority)
                {
                    //Находим скилл, в очереди которого стоит вызов с макс приоритетом и длительностью
                    Skill skillWithMaxPriorCall = skills.Find(s => s.CallsInQueue.Contains(c));
                    Operator freeOper = null;

                    //Находим свободного оператора
                    if (skillWithMaxPriorCall != null)
                        freeOper = GetFreeOperator(skillWithMaxPriorCall.Operators);

                    //Передаем вызов оператору
                    if (freeOper != null)
                    {
                        PassCallFromQueueToOperator(skillWithMaxPriorCall, freeOper, currentTime);
                        SetOperStatusInAllSkills(skills, freeOper);
                    }
                }
            }
        }

        private int GetFreeCallID(List<Skill> skills)
        {
            int newCallID = 0;
            Random random = new Random();

            while (true)
            {
                newCallID = random.Next(1, 999999);

                var ActiveCalls = skills.Select(s => s.ActiveCalls).Select(lc => lc.Select(c => c).Where(c => c.CallID == newCallID));
                var CallsInQueue = skills.Select(s => s.CallsInQueue).Select(lc => lc.Select(c => c).Where(c => c.CallID == newCallID));

                foreach (var actC in ActiveCalls)
                {
                    if (actC.Count() == 0)
                        foreach (var queueC in CallsInQueue)
                        {
                            if (queueC.Count() == 0)
                                return newCallID;
                        }
                }
            }
        }

        public int UpdateSkillData(List<Skill> skills, Skill currentSkill)
        {
            int abandoned = 0;

            for (int j = 0; j < currentSkill.ActiveCalls.Count; j++)
            {
                currentSkill.ActiveCalls[j].Duration++;

                //Завершаем вызов, если он по длительности больше чем заданная длительность вызова
                if (currentSkill.ActiveCalls[j].Duration > currentSkill.ActiveCalls[j].TalkFullDuration)
                {
                    Operator oper = currentSkill.Operators.Where(o => o.call != null && o.call.CallID == currentSkill.ActiveCalls[j].CallID).FirstOrDefault();

                    if (oper != null)
                    {
                        oper.CurrentStatus = "Ready";
                        oper.call = null;
                        oper.TimeInCurrentStatus = 0;

                        SetOperStatusInAllSkills(skills, oper);
                    }

                    currentSkill.HistoricalCalls.Add(currentSkill.ActiveCalls[j]);
                    currentSkill.ActiveCalls.Remove(currentSkill.ActiveCalls[j]);
                }
            }

            for (int j = 0; j < currentSkill.CallsInQueue.Count; j++)
            {
                currentSkill.CallsInQueue[j].Duration++;

                //Вызов потерян
                if (currentSkill.CallsInQueue[j].Duration > currentSkill.CallsInQueue[j].TalkFullDuration)
                {
                    currentSkill.HistoricalCalls.Add(currentSkill.CallsInQueue[j]);
                    currentSkill.CallsInQueue.Remove(currentSkill.CallsInQueue[j]);

                    abandoned++;
                }
            }

            for (int k = 0; k < currentSkill.Operators.Count; k++)
            {
                currentSkill.Operators[k].TimeInCurrentStatus++;
            }

            return abandoned;
        }

        private void SetOperStatusInAllSkills(List<Skill> skills, Operator operMaster)
        {
            foreach (var skill in skills)
            {
                foreach (var oper in skill.Operators)
                {
                    if (oper.Name == operMaster.Name)
                    {
                        oper.CurrentStatus = operMaster.CurrentStatus;
                        oper.TimeInCurrentStatus = operMaster.TimeInCurrentStatus;
                    }
                }
            }
        }

        public Operator GetFreeOperator(List<Operator> opers)
        {
            foreach (var oper in opers)
            {
                if (oper.CurrentStatus == "Ready")
                    return oper;
            }

            return null;
        }

        private void PassCallFromQueueToOperator(Skill skill, Operator oper, int currentTime)
        {
            //Получаем данный звонок из очереди
            Call callInQueue = skill.CallsInQueue.OrderBy(c => c.Duration).FirstOrDefault();

            if (callInQueue != null)
            {
                //Если есть свободные операторы, то в цикле выделяем операторов под данные вызовы в очереди
                //Извлечь данный вызов из очереди
                skill.CallsInQueue.Remove(callInQueue);

                if (callInQueue.Duration <= skill.statistic.SLSeconds)
                    skill.statistic.SLCalls++;

                //Добавить его в активные вызовы
                callInQueue.Duration = 0;
                callInQueue.CallAnsweredTime = currentTime;
                skill.ActiveCalls.Add(callInQueue);

                //Добавить вызов свободному оператору
                oper.call = callInQueue;
                oper.CurrentStatus = "Talking";
                oper.TimeInCurrentStatus = 0;

                skill.statistic.CallAnswered++;
            }
        }

        public void TryRaisePriority(Skill currentSkill, int maxWaitTimeBeforeRaisePrior, int raisedPrior)
        {
            List<Call> callRaisePriority = currentSkill.CallsInQueue.Select(c => c).Where(c => c.Duration > maxWaitTimeBeforeRaisePrior).ToList();

            foreach (var call in callRaisePriority)
            {
                call.CallPriority = raisedPrior;
            }
        }

        public string GetStatistics(int currentTime, List<Skill> skills, bool showOperStat = false)
        {
            StringBuilder statistics = new StringBuilder();
            TimeConverter timeConverter = new TimeConverter();

            foreach (var skill in skills)
            {
                statistics.Append($"Statistics in skill {skill.SkillName}\n");
                statistics.Append($"Offered calls \t{skill.statistic.CallsOffered}\n");
                statistics.Append($"Abandoned calls \t{skill.statistic.AbandonedCalls}\n");
                statistics.Append($"Calls answered \t{skill.statistic.CallAnswered}\n");
                statistics.Append($"SLCalls \t\t{skill.statistic.SLCalls}\n");

                if (skill.statistic.CallsOffered > 0)
                    statistics.Append($"SL \t\t{Math.Round(((1.0 * skill.statistic.SLCalls / skill.statistic.CallsOffered) * 100), 2)}%\n");

                statistics.Append($"--------------------------------------\n");

                statistics.Append($"Available operators \t{skill.Operators.Where(o => o.CurrentStatus == "Ready").Count()}\n");
                statistics.Append($"Operator talking in skill \t{skill.ActiveCalls.Count()}\n");
                statistics.Append($"Queue in skill \t\t{ skill.CallsInQueue.Count() }\n");
                statistics.Append($"Longest call in queue \t {timeConverter.Convert(skill.CallsInQueue.Select(c => c.Duration).OrderByDescending(c => c).FirstOrDefault(), null, null, null)}\n");
                statistics.Append($"--------------------------------------\n");

                if (showOperStat)
                {
                    foreach (var oper in skill.Operators)
                    {
                        if (oper.CurrentStatus == "Talking" && oper.call != null)
                            statistics.Append($"{oper.Name} in status {oper.CurrentStatus} in skill {skill.SkillName} time = {timeConverter.Convert(oper.TimeInCurrentStatus, null, null, null)} \n");
                        else
                        if (oper.CurrentStatus == "Talking" && oper.call == null)
                        {
                            foreach (var skillSearch in skills)
                            {
                                if (skillSearch.SkillName != skill.SkillName)
                                    foreach (var operSearch in skillSearch.Operators)
                                    {
                                        if (operSearch.call != null && operSearch.Name == oper.Name)
                                            statistics.Append($"{oper.Name} in status {oper.CurrentStatus} in skill {skillSearch.SkillName} time = {timeConverter.Convert(oper.TimeInCurrentStatus, null, null, null)} \n");
                                    }
                            }
                        }
                        else
                            statistics.Append($"{oper.Name} in status {oper.CurrentStatus} time = {timeConverter.Convert(oper.TimeInCurrentStatus, null, null, null)}\n");
                    }
                    statistics.Append($"--------------------------------------\n");
                }
            }

            foreach (var skill in skills)
            {
                statistics.Append($"-  HISTORICAL DATA {skill.SkillName} -\n");

                foreach (var call in skill.HistoricalCalls)
                {
                    //Console.WriteLine($"Call {call.CallID} int skill {skill.SkillName} was offered at {ConvertSecToHumanReadyFormat(call.CallOfferedTime)} and was answered at {ConvertSecToHumanReadyFormat(call.CallAnsweredTime)} with duration {ConvertSecToHumanReadyFormat(call.TalkFullDuration)}");
                }

                if (skill.statistic.Calls.Count > 0)
                    statistics.Append($"Average Talk Time = { timeConverter.Convert(skill.statistic.Calls.Sum(c => c.Duration) / skill.statistic.Calls.Count, null, null, null) }\n");

                statistics.Append($"--------------------------------------\n");
            }

            return statistics.ToString();
        }

        public Skill ResetSkill(Skill skill)
        {
            Skill newSkill = new Skill(skill.SkillName, skill.Operators, skill.Priority, skill.CallAllocation);
            newSkill.CallsAllocationInterval = skill.CallsAllocationInterval;

            for (int i = 0; i < newSkill.Operators.Count; i++)
            {
                newSkill.Operators[i] = ResetOperator(newSkill.Operators[i]);
            }

            return newSkill;
        }

        private Operator ResetOperator(Operator oper)
        {
            Operator newOper = new Operator(oper.Name);

            return newOper;
        }

        public ObservableCollection<Skill> ResetSkills(ObservableCollection<Skill> skills)
        {
            ObservableCollection<Skill> newCollection = new ObservableCollection<Skill>();

            for (int i = 0; i < skills.Count; i++)
            {
                newCollection.Add(ResetSkill(skills[i]));
            }

            return newCollection;
        }

        
}
}