using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CallFlowPriorityConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(() => StartSimulation());

            Console.ReadLine();
        }

        public static (List<int>, List<int>) GenerateCallsAllocation(int intervalSeconds, int minTalkTimePer, int maxTalkTimePer, int callsCount)
        {
            List<int> callsAllocation = new List<int>();
            List<int> callsTalkTimeAllocation = new List<int>();

            Random randomSeconds = new Random();

            while (callsCount > 0)
            {
                int newRandom = randomSeconds.Next(1, intervalSeconds);
                if (callsAllocation.Contains(newRandom))
                    continue;

                callsAllocation.Add(newRandom);
                callsTalkTimeAllocation.Add(randomSeconds.Next(minTalkTimePer, maxTalkTimePer));

                callsCount--;
            }

            callsAllocation.Sort();

            return (callsAllocation, callsTalkTimeAllocation);
        }

        public static (List<int>, List<int>) GenerateCallsAllocation(Dictionary<int, int> callDurationAllocation, int intervalSeconds, int minTalkTimePer, int maxTalkTimePer)
        {
            List<int> callsAllocation = new List<int>();
            List<int> callsTalkTimeAllocation = new List<int>();

            Random randomSeconds = new Random();

            while(callDurationAllocation.Values.Select(v => v).Sum() > callsAllocation.Count())
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

        public static List<Operator> GenerateOperators(int countOfOperators, int startIndex)
        {
            List<Operator> operators = new List<Operator>();

            for (int i = startIndex; i < countOfOperators + startIndex; i++)
                operators.Add(new Operator($"Operator{i + 1}"));

            return operators;
        }

        static async void StartSimulation()
        {
            Console.CursorVisible = false;

            const int MaxPeriodTime = 86400;

            const int periodSec = 900;
            const int minTalkTimePeriod = 10;
            const int maxTalkTimePeriod = 300;
            int callsCountIn15m = 100;

            int currentTime = 1;
            int periodRequestData = periodSec;

            Random random = new Random();

            //Генерим случайное число, если в интервале, % заполнения еще меньше чем заданный по словарю, добавляем в коллекцию вызов

            List<Skill> skills = new List<Skill>();

            skills.Add(new Skill("SG_50", GenerateOperators(50, 0), 4, GenerateCallsAllocation(
                //Словарь распределения вызовов по времени
                new Dictionary<int, int>
                {
                    {0, 35},
                    {60, 40},
                    {120, 25},
                    {180, 19},
                    {240, 10},
                    {300, 7},
                    {360, 5},
                    {420, 3},
                    {480, 2},
                    {540, 1},
                    {600, 1},
                    {660, 2}
                }, periodSec, 10, 900)));

            skills.Add(new Skill("SG_21", GenerateOperators(10, 45), 5, GenerateCallsAllocation(
                //Словарь распределения вызовов по времени
                new Dictionary<int, int>
                {
                    {0, 9},
                    {60, 5},
                    {120, 8},
                    {180, 7},
                    {240, 6},
                    {300, 4},
                    {360, 4},
                    {420, 2},
                    {480, 1},
                    {540, 1},
                    {600, 1},
                    {660, 2}
                }, periodSec, 10, 900)));

            skills = skills.OrderBy(s => s.Priority).ToList();

            while(true)
            //while (currentTime < MaxPeriodTime)
            {
                foreach (var skill in skills)
                {
                    //Обновляем время в операторах и скиллах
                    skill.statistic.AbandonedCalls += UpdateSkillData(skills, skill);

                    if (skill.SkillName == "SG_21")
                        TryRaisePriority(skill, 60, 2);

                    CheckQueueInSkills(skills, currentTime);

                    //Reload calls data
                    if (currentTime == periodRequestData)
                    {
                        switch (skill.SkillName)
                        {
                            case "SG_50":
                                skill.LoadNewCallsAllocation(GenerateCallsAllocation(
                                    new Dictionary<int, int>
                                    {
                                        {0, 35},
                                        {60, 40},
                                        {120, 25},
                                        {180, 19},
                                        {240, 10},
                                        {300, 7},
                                        {360, 5},
                                        {420, 3},
                                        {480, 2},
                                        {540, 1},
                                        {600, 1},
                                        {660, 2}
                                    }, periodSec, 10, 900));
                                break;
                            case "SG_21":
                                skill.LoadNewCallsAllocation(GenerateCallsAllocation(
                                    new Dictionary<int, int>
                                    {
                                        {0, 9},
                                        {60, 5},
                                        {120, 8},
                                        {180, 7},
                                        {240, 6},
                                        {300, 4},
                                        {360, 4},
                                        {420, 2},
                                        {480, 1},
                                        {540, 1},
                                        {600, 1},
                                        {660, 2}
                                    }, periodSec, 10, 900));
                                break;
                            default:
                                skill.LoadNewCallsAllocation(GenerateCallsAllocation(periodSec, minTalkTimePeriod, maxTalkTimePeriod, callsCountIn15m));
                                break;
                        }

                        periodRequestData += periodSec;
                    }

                    foreach (var call in skill.CallAllocation.Item1.Where(c => c == currentTime - (periodRequestData - periodSec)).ToList())
                    {
                        skill.statistic.CallsOffered++;

                        //Ищем свободного оператора
                        Operator oper = GetFreeOperator(skill.Operators);

                        //Добавляем новый звонок в очередь на скилле
                        if (oper == null)
                            PutCallToQueue(skills, skill, currentTime - (periodRequestData - periodSec));
                        else
                            //Добавляем звонок оператору
                            AnswerCall(oper, skills, skill, currentTime - (periodRequestData - periodSec));
                    }
                }

                if (currentTime % 10 == 0 || currentTime == 1)
                //if(currentTime >= MaxPeriodTime-1)
                    PrintStatistics(currentTime, skills);

                await Task.Delay(1000);
                currentTime++;
            }
        }

        private static void AnswerCall(Operator oper, List<Skill> skills, Skill currentSkill, int currentTime)
        {
            Random random = new Random();

            //Добавляем звонок оператору
            oper.call = new Call(GetFreeCallID(skills), currentSkill.CallAllocation.Item2[currentSkill.CallAllocation.Item1.IndexOf(currentTime)], currentTime, currentSkill.Priority);

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

        private static void PutCallToQueue(List<Skill> skills, Skill skill, int currentTime)
        {
            Call call = new Call(GetFreeCallID(skills), skill.CallAllocation.Item2[skill.CallAllocation.Item1.IndexOf(currentTime)], currentTime, skill.Priority);

            call.CallOfferedTime = currentTime;

            skill.CallsInQueue.Add(call);
        }

        private static void CheckQueueInSkills(List<Skill> skills, int currentTime)
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
                    if(freeOper != null)
                    {
                        PassCallFromQueueToOperator(skillWithMaxPriorCall, freeOper, currentTime);
                        SetOperStatusInAllSkills(skills, freeOper);
                    }
                }
            }
        }

        private static int GetFreeCallID(List<Skill> skills)
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

        private static int UpdateSkillData(List<Skill> skills, Skill currentSkill)
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

        static void SetOperStatusInAllSkills(List<Skill> skills, Operator operMaster)
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

        static void PrintStatistics(int currentTime, List<Skill> skills, bool showOperStat = false)
        {
            Console.Clear();

            Console.WriteLine($"Time {ConvertSecToHumanReadyFormat(currentTime)}");
            Console.WriteLine($"--------------------------------------");

            foreach (var skill in skills)
            {
                Console.WriteLine($"-= Statistics in skill {skill.SkillName} =-");
                Console.WriteLine($"Offered calls \t\t{skill.statistic.CallsOffered}");
                Console.WriteLine($"Abandoned calls \t{skill.statistic.AbandonedCalls}");
                Console.WriteLine($"Calls answered \t\t{skill.statistic.CallAnswered}");
                Console.WriteLine($"SLCalls \t\t{skill.statistic.SLCalls}");

                if (skill.statistic.CallsOffered > 0)
                    Console.WriteLine($"-= SL \t\t\t{Math.Round(((1.0 * skill.statistic.SLCalls / skill.statistic.CallsOffered) * 100), 2)}% =-");

                Console.WriteLine($"--------------------------------------");

                //Console.WriteLine($"Current calls in skill \t\t{skill.CallsInQueue.Count() + skill.ActiveCalls.Count()}");

                Console.WriteLine($"Available operators \t\t{skill.Operators.Where(o => o.CurrentStatus == "Ready").Count()}");
                Console.WriteLine($"Operator talking in skill \t{skill.ActiveCalls.Count()}");
                Console.WriteLine($"Queue in skill \t\t\t{ skill.CallsInQueue.Count() }\t{ GetBar(skill.CallsInQueue.Count(), "! ") }");
                Console.WriteLine($"Longest call in queue \t\t {ConvertSecToHumanReadyFormat(skill.CallsInQueue.Select(c => c.Duration).OrderByDescending(c => c).FirstOrDefault())}");
                Console.WriteLine($"--------------------------------------");

                if (showOperStat)
                    foreach (var oper in skill.Operators)
                    {
                        if (oper.CurrentStatus == "Talking" && oper.call != null)
                            Console.WriteLine($"{oper.Name} in status {oper.CurrentStatus} in skill {skill.SkillName} time = {ConvertSecToHumanReadyFormat(oper.TimeInCurrentStatus)}");
                        else
                        if (oper.CurrentStatus == "Talking" && oper.call == null)
                        {
                            foreach (var skillSearch in skills)
                            {
                                if (skillSearch.SkillName != skill.SkillName)
                                    foreach (var operSearch in skillSearch.Operators)
                                    {
                                        if (operSearch.call != null && operSearch.Name == oper.Name)
                                            Console.WriteLine($"{oper.Name} in status {oper.CurrentStatus} in skill {skillSearch.SkillName} time = {ConvertSecToHumanReadyFormat(oper.TimeInCurrentStatus)}");
                                    }
                            }
                        }
                        else
                            Console.WriteLine($"{oper.Name} in status {oper.CurrentStatus} time = {ConvertSecToHumanReadyFormat(oper.TimeInCurrentStatus)}");
                    }

                Console.WriteLine($"--------------------------------------");
            }

            foreach (var skill in skills)
            {
                Console.WriteLine($"--------- HISTORICAL DATA {skill.SkillName} ------------");

                foreach (var call in skill.HistoricalCalls)
                {
                    //Console.WriteLine($"Call {call.CallID} int skill {skill.SkillName} was offered at {ConvertSecToHumanReadyFormat(call.CallOfferedTime)} and was answered at {ConvertSecToHumanReadyFormat(call.CallAnsweredTime)} with duration {ConvertSecToHumanReadyFormat(call.TalkFullDuration)}");
                }

                if(skill.statistic.Calls.Count > 0)
                    Console.WriteLine($"Average Talk Time = { ConvertSecToHumanReadyFormat(skill.statistic.Calls.Sum(c => c.Duration) / skill.statistic.Calls.Count) }");

                Console.WriteLine($"--------------------------------------");
            }

            Console.SetCursorPosition(0, 0);
        }

        static Operator GetFreeOperator(List<Operator> opers)
        {
            foreach (var oper in opers)
            {
                if (oper.CurrentStatus == "Ready")
                    return oper;
            }

            return null;
        }

        static void PassCallFromQueueToOperator(Skill skill, Operator oper, int currentTime)
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

        static string GetBar(int maxCount, string symbol)
        {
            string output = "";

            for (int i = 0; i < maxCount; i++)
            {
                output += symbol;
            }

            return output;
        }

        static string ConvertSecToHumanReadyFormat(int sec)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(sec);

            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        static void TryRaisePriority(Skill currentSkill, int maxWaitTimeBeforeRaisePrior, int raisedPrior)
        {
            List<Call> callRaisePriority = currentSkill.CallsInQueue.Select(c => c).Where(c => c.Duration > maxWaitTimeBeforeRaisePrior).ToList();

            foreach (var call in callRaisePriority)
            {
                call.CallPriority = raisedPrior;
            }
        }
    }

    public class Operator
    {
        public string Name { get; set; }
        public string CurrentStatus { get; set; }
        public int TimeInCurrentStatus { get; set; }
        public Call call { get; set; }

        public Operator(string name)
        {
            Name = name;
            CurrentStatus = "Ready";
            TimeInCurrentStatus = 0;
        }
    }

    public class Skill
    {
        public string SkillName { get; set; }
        public List<Call> ActiveCalls { get; set; }
        public List<Call> CallsInQueue { get; set; }
        public List<Call> HistoricalCalls { get; set; }
        public List<Operator> Operators { get; set; }
        public int Priority { get; set; }
        public (List<int>, List<int>) CallAllocation { get; set; }
        public Statistics statistic { get; set; }


        public Skill(string name, List<Operator> opers, int priority, (List<int>, List<int>) callAlloc)
        {
            SkillName = name;
            Operators = opers;
            Priority = priority;
            CallAllocation = callAlloc;

            if (ActiveCalls == null)
                ActiveCalls = new List<Call>();

            if (CallsInQueue == null)
                CallsInQueue = new List<Call>();

            if (statistic == null)
                statistic = new Statistics();

            if (HistoricalCalls == null)
                HistoricalCalls = new List<Call>();
        }

        public void LoadNewCallsAllocation((List<int>, List<int>) alloc)
        {
            CallAllocation = alloc;
        }
    }

    public class Call
    {
        public int CallID { get; set; }
        public int TalkFullDuration { get; set; }
        public int Duration { get; set; }
        public int CallOfferedTime { get; set; }
        public int CallAnsweredTime { get; set; }
        public int CallPriority { get; set; }

        public Call(int callID, int fullCallDuration, int offeredTime, int priority)
        { 
            CallID = callID;
            TalkFullDuration = fullCallDuration;
            Duration = 0;
            CallAnsweredTime = 0;
            CallOfferedTime = offeredTime;
            CallPriority = priority;
        }
    }

    public class Statistics
    {
        public int CallsOffered { get; set; }
        public int AbandonedCalls { get; set; }
        public int CallAnswered { get; set; }
        public int SLCalls { get; set; }
        public int SL { get; set; }
        public int SLSeconds { get; set; }
        public List<Call> Calls { get; set; }

        public Statistics()
        {
            CallsOffered = 0;
            AbandonedCalls = 0;
            CallAnswered = 0;
            SLCalls = 0;
            SL = 100;
            SLSeconds = 10;
            Calls = new List<Call>();
        }
    }
}