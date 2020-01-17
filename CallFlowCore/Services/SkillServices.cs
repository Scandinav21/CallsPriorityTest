using CallFlowModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CallFlowCore.Converters;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading.Tasks;

namespace CallFlowCore.Services
{
    public class SkillServices : ISkillServices
    {
        public List<Operator> GenerateOperators(int countOfOperators, int startIndex)
        {
            List<Operator> operators = new List<Operator>();

            if (startIndex < 1)
            {
                MessageBox.Show("Введите корректный индекс первого оператора > 0", "Ошибка начального индекса оператора", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            for (int i = startIndex; i < countOfOperators + startIndex; i++)
                operators.Add(new Operator($"Operator{i}"));

            return operators;
        }

        public async Task<(List<int>, List<int>)> GenerateCallsAllocation(Dictionary<int, int> callDurationAllocation, int startAllocationTime, int intervalSeconds, int minTalkTimePer, int maxTalkTimePer)
        {
            List<int> callsAllocation = new List<int>();
            List<int> callsTalkTimeAllocation = new List<int>();

            await Task.Delay(100);
            Random randomSeconds = new Random();

            while (callDurationAllocation.Values.Select(v => v).Sum() > callsAllocation.Count())
            {
                //Генерим время поступления вызова
                int newCallOfferedTime = randomSeconds.Next(startAllocationTime, startAllocationTime + intervalSeconds);

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

                await Task.Delay(10);
            }

            callsAllocation.Sort();

            return (callsAllocation, callsTalkTimeAllocation);
        }

        public void AnswerCall(Operator oper, List<Skill> skills, Skill currentSkill, int currentTime)
        {
            //Добавляем звонок оператору
            oper.call = new Call(GetFreeCallID(skills), currentSkill.CallAllocation.Item2[currentSkill.CallAllocation.Item1.IndexOf(currentTime)], currentTime, currentSkill.Priority);

            oper.call.CallAnsweredTime = currentTime;

            //Меняем статусы оператора
            oper.CurrentStatus = "Talking";
            oper.TimeInCurrentStatus = 0;

            //Добавляем звонок в активные по скиллу
            currentSkill.ActiveCalls.Add(oper.call);
            currentSkill.CallAllocation.Item2.RemoveAt(currentSkill.CallAllocation.Item1.FindIndex(i => i == currentTime));
            currentSkill.CallAllocation.Item1.RemoveAt(currentSkill.CallAllocation.Item1.FindIndex(i => i == currentTime));

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
            skill.CallAllocation.Item2.RemoveAt(skill.CallAllocation.Item1.FindIndex(i => i == currentTime));
            skill.CallAllocation.Item1.RemoveAt(skill.CallAllocation.Item1.FindIndex(i => i == currentTime));
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

                //callsSamePriority = callsSamePriority.OrderBy(c => c.Duration).ToList();
                Call[] callsSamePriorityArr = callsSamePriority.OrderBy(c => c.Duration).ToArray();

                foreach (var c in callsSamePriorityArr)
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
                        return;
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
                newCallID = random.Next(1000000000, 2147483646);

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
            if(opers.Select(o => o).Where(o => o.CurrentStatus == "Ready").Count() > 0)
            {
                Dictionary<string, int> operatorsDict = GetOperatorsDict(opers, "Ready");

                Random random = new Random();
                int randomInt = 0;

                try
                {
                    int maxTries = 100;
                    int currentTry = 0;

                    while (currentTry < maxTries)
                    {
                        randomInt = random.Next(operatorsDict.Values.Min(), operatorsDict.Values.Max());

                        Operator freeOper = opers.Find(o => o.Name == "Operator" + randomInt);

                        if (freeOper.CurrentStatus == "Ready")
                            return freeOper;

                        currentTry++;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка во время поиска свободного оператора\n{ e.Message} randomInt = {randomInt} where min oper index = {operatorsDict.Values.Min() - 1}, max {operatorsDict.Values.Max()}" , 
                        "Ошибка получения оператора", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }

            return null;
        }

        public Dictionary<string, int> GetOperatorsDict(List<Operator> opers, string status = null)
        {
            Regex regex = new Regex(@"\d+");
            Dictionary<string, int> operatorsDict = new Dictionary<string, int>();

            foreach (var oper in opers)
            {
                if (regex.IsMatch(oper.Name))
                {
                    if(status == null || (status != null && oper.CurrentStatus == status))
                        operatorsDict.Add(oper.Name, Convert.ToInt32(regex.Match(oper.Name).Value));
                }
            }

            return operatorsDict;
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

        public void TryRaiseCallPriority(Skill currentSkill, int maxWaitTimeBeforeRaisePrior, int raisedPrior)
        {
            List<Call> callRaisePriority = currentSkill.CallsInQueue.Select(c => c).Where(c => c.Duration > maxWaitTimeBeforeRaisePrior).ToList();

            foreach (var call in callRaisePriority)
            {
                call.CallPriority = raisedPrior;
            }
        }

        public void TryRaiseSkillPriority(ObservableCollection<Skill> skills, Skill currentSkill)
        {
            if(currentSkill.PriorCondition != null && currentSkill.PriorCondition.priorityWhenQ != 0 && 
                (currentSkill.PriorCondition.queueMultiple || currentSkill.PriorCondition.queueSingle) && 
                currentSkill.PriorCondition.queueVal > 0 && currentSkill.PriorCondition.skills != null && 
                currentSkill.PriorCondition.skills.Count > 0)
            {
                //Если очередь только на каком-либо одном скилле
                if(currentSkill.PriorCondition.queueSingle)
                {
                    foreach (var skill in currentSkill.PriorCondition.skills)
                    {
                        if (skills.Select(s => s)
                            .Where(s => s.SkillName == skill && s.CallsInQueue.Count() > currentSkill.PriorCondition.queueVal && 
                            s.SkillName != currentSkill.SkillName).Count() > 0)
                        {
                            TryRaiseCallPriority(currentSkill, 0, currentSkill.PriorCondition.priorityWhenQ);
                            return;
                        }
                    }
                }

                //Если очередь должна быть на всех скиллах списка
                if(currentSkill.PriorCondition.queueMultiple)
                {
                    int skillsFetched = 0;

                    foreach (var skill in skills)
                    {
                        if (currentSkill.PriorCondition.skills.Contains(skill.SkillName))
                            skillsFetched++;
                    }

                    if (skillsFetched == currentSkill.PriorCondition.skills.Count)
                        TryRaiseCallPriority(currentSkill, 0, currentSkill.PriorCondition.priorityWhenQ);
                }
            }
        }

        public string GetStatistics(int currentTime, List<Skill> skills, Skill loadStatisticsFromSkill = null, bool showBrief = false, bool showOperStat = false)
        {
            StringBuilder statistics = new StringBuilder();
            TimeConverter timeConverter = new TimeConverter();

            foreach (var skill in skills)
            {
                if (loadStatisticsFromSkill != null && loadStatisticsFromSkill.SkillName != skill.SkillName)
                    continue;

                statistics.Append($"Statistics in skill {skill.SkillName}\n");
                statistics.Append($"Offered calls \t{skill.statistic.CallsOffered}\n");
                statistics.Append($"Queue in skill \t{ skill.CallsInQueue.Count() }\n");

                if (!showBrief)
                {
                    statistics.Append($"Abandoned calls \t{skill.statistic.AbandonedCalls}\n");
                    statistics.Append($"Calls answered \t{skill.statistic.CallAnswered}\n");
                    statistics.Append($"SLCalls \t\t{skill.statistic.SLCalls}\n");
                }

                if (skill.statistic.CallsOffered > 0)
                    statistics.Append($"SL \t\t{Math.Round(((1.0 * skill.statistic.SLCalls / skill.statistic.CallsOffered) * 100), 2)}%\n");

                statistics.Append($"--------------------------------------\n");

                if (!showBrief)
                {
                    statistics.Append($"Available operators \t{skill.Operators.Where(o => o.CurrentStatus == "Ready").Count()}\n");
                    statistics.Append($"Operator talking in skill \t{skill.ActiveCalls.Count()}\n");
                    statistics.Append($"Longest call in queue \t {timeConverter.Convert(skill.CallsInQueue.Select(c => c.Duration).OrderByDescending(c => c).FirstOrDefault(), null, null, null)}\n");
                    statistics.Append($"--------------------------------------\n");
                }

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

            if (!showBrief)
                foreach (var skill in skills)
                {
                    if (loadStatisticsFromSkill != null && loadStatisticsFromSkill.SkillName != skill.SkillName)
                        continue;

                    statistics.Append($"-----HISTORICAL DATA {skill.SkillName}-----\n");

                    foreach (var call in skill.HistoricalCalls)
                    {
                        statistics.Append($"Call {call.CallID} in skill {skill.SkillName} was offered at {timeConverter.Convert(call.CallOfferedTime, null, null, null)} and was answered at {timeConverter.Convert(call.CallAnsweredTime, null, null, null)} with duration {timeConverter.Convert(call.TalkFullDuration, null, null, null)} and priority {call.CallPriority}\n");
                    }

                    if (skill.statistic.Calls.Count > 0)
                        statistics.Append($"Average Talk Time = { timeConverter.Convert(skill.statistic.Calls.Sum(c => c.Duration) / skill.statistic.Calls.Count, null, null, null) }\n");

                    statistics.Append($"--------------------------------------\n");
                }

            return statistics.ToString();
        }

        public async Task<Skill> ResetSkill(Skill skill)
        {
            skill.ActiveCalls = new List<Call>();
            skill.CallsInQueue = new List<Call>();
            skill.HistoricalCalls = new List<Call>();
            skill.statistic = new Statistics();
            skill.CallAllocation = await Task.Run(() => GenerateCallsAllocation(skill.CallsDurationAllocation, 1, skill.CallsAllocationInterval, skill.MinTalkTimeDur, skill.MaxTalkTimeDur));

            for (int i = 0; i < skill.Operators.Count; i++)
            {
                skill.Operators[i] = ResetOperator(skill.Operators[i]);
            }

            return skill;
        }

        private Operator ResetOperator(Operator oper)
        {
            Operator newOper = new Operator(oper.Name);

            return newOper;
        }

        public async Task<ObservableCollection<Skill>> ResetSkills(ObservableCollection<Skill> skills)
        {
            ObservableCollection<Skill> newCollection = new ObservableCollection<Skill>();

            for (int i = 0; i < skills.Count; i++)
            {
                newCollection.Add(await ResetSkill(skills[i]));
            }

            return newCollection;
        }


        public PriorityConditions GetPriorityConditions(string query)
        {
            PriorityConditions priorityConditions = new PriorityConditions();

            string regexp;
            try
            {
                //Проверяем есть ли условие по очереди на скиллах
                if ((bool)CheckRegexIsMatch(query, @"queue \w+"))
                {
                    //Проверяем очередь только на одном из скиллов
                    if ((bool)CheckRegexIsMatch(query, @"queue in \(\w+"))
                        priorityConditions.queueSingle = true;

                    //Проверяем очередь на всех скиллах списка
                    if ((bool)CheckRegexIsMatch(query, @"queue in each \(\w+"))
                        priorityConditions.queueMultiple = true;

                    //Получаем список скиллов
                    regexp = @"\((\w+\,{0,})+\)";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                    {
                        string skillsStr = (string)CheckRegexIsMatch(query, regexp, true);
                        priorityConditions.skills = ParseSkills(skillsStr, ',', new char[] { '(', ')' });
                    }

                    //Получаем знак сравнения для очереди
                    regexp = @"\) <|>|<=|>=|== \d+";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                        priorityConditions.signQueue = (string)CheckRegexIsMatch(query, regexp, true);

                    //Получаем значение очереди
                    regexp = @"\) .{1,2} \d+";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                    {
                        priorityConditions.queueVal = GetIntAfterSign(query, regexp);
                    }

                    regexp = @"queue.*\) (<|>|<=|>=|=) \d+ then priority = \d+";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                    {
                        priorityConditions.priorityWhenQ = GetIntAfterSign(query, regexp);
                    }
                }

                if ((bool)CheckRegexIsMatch(query, @"timewait > \d+"))
                {
                    priorityConditions.timeWait = true;

                    regexp = @"timewait <|>|<=|>=|== \d+";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                        priorityConditions.signTimeWait = (string)CheckRegexIsMatch(query, regexp, true);

                    regexp = @"timewait .{1,2} \d+";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                    {
                        priorityConditions.timeWaitVal = GetIntAfterSign(query, regexp);
                    }

                    regexp = @"timeWait.(<|>|<=|>=|=) \d+ then priority = \d+";
                    if ((bool)CheckRegexIsMatch(query, regexp))
                    {
                        priorityConditions.priorityWhenTimeW = GetIntAfterSign(query, regexp);
                    }
                }

                regexp = @" and ";
                if ((bool)CheckRegexIsMatch(query, regexp))
                    priorityConditions.unitCondition = 1;

                regexp = @" or ";
                if ((bool)CheckRegexIsMatch(query, regexp))
                    priorityConditions.unitCondition = -1;
            }
            catch(Exception e)
            {
                MessageBox.Show("Не удалось распарсить запрос, описывающий приоритизацию\n" + e.Message, "Ошибка приоритизации", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return priorityConditions;
        }

        private object CheckRegexIsMatch(string str, string regexStr, bool getMatchVal = false)
        {
            Regex regex = new Regex(regexStr, RegexOptions.IgnoreCase);

            if (!getMatchVal)
                return regex.IsMatch(str);
            else
                if (getMatchVal)
                return regex.Match(str).Value;
            else
                return false;
        }

        private int GetIntAfterSign(string str, string regexStr)
        {
            int result = -1;

            Regex regex = new Regex(regexStr, RegexOptions.IgnoreCase);

            string tempVal = regex.Match(str).Value;
            IEnumerable<char> tempValArr = tempVal.Reverse();
            tempVal = "";

            foreach (var item in tempValArr)
            {
                tempVal += item.ToString();
            }

            regex = new Regex(@"^\d+");
            tempValArr = regex.Match(tempVal).Value.Reverse();

            tempVal = "";

            foreach (var item in tempValArr)
            {
                tempVal += item.ToString();
            }

            Int32.TryParse(tempVal, out result);


            return result;
        }

        private List<string> ParseSkills(string skills, char separator, char[] curves)
        {
            List<string> skillsList = new List<string>();

            skills = skills.Trim(curves[0]);
            skills = skills.Trim(curves[1]);

            string[] skillsSplitted = skills.Split(separator);

            foreach (var skill in skillsSplitted)
            {
                skillsList.Add(skill);
            }

            return skillsList;
        }

    }
}
