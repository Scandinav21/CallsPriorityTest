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

        public static List<Operator> GenerateOperators(int countOfOperators, int startIndex)
        {
            List<Operator> operators = new List<Operator>();

            for (int i = startIndex; i < countOfOperators + startIndex; i++)
                operators.Add(new Operator($"Operator{i + 1}"));

            return operators;
        }

        static async void StartSimulation()
        {
            const int periodSec = 900;
            const int minTalkTimePeriod = 10;
            const int maxTalkTimePeriod = 300;
            const int operatorsCountInSkill = 10;
            int callsCountIn15m = 100;

            int currentTime = 1;
            int periodRequestData = periodSec;

            Random random = new Random();

            List<Skill> skills = new List<Skill>();
            skills.Add(new Skill("SG_50", GenerateOperators(10, 0), 4, GenerateCallsAllocation(periodSec, 10, 300, 100)));
            skills.Add(new Skill("SG_21", GenerateOperators(10, 3), 5, GenerateCallsAllocation(periodSec, 60, 600, 50)));
            skills = skills.OrderBy(s => s.Priority).ToList();

            while (true)
            {
                foreach (var skill in skills)
                {
                    //Обновляем время в операторах и скиллах
                    skill.statistic.AbandonedCalls += UpdateSkillData(skills, skill);

                    CheckQueueInSkills(skills);

                    if (currentTime == periodRequestData)
                    {
                        switch(skill.SkillName)
                        {
                            case "SG_50":
                                skill.LoadNewCallsAllocation(GenerateCallsAllocation(periodSec, 10, 300, 100));
                                break;
                            case "SG_21":
                                skill.LoadNewCallsAllocation(GenerateCallsAllocation(periodSec, 50, 900, 50));
                                break;
                            default:
                                skill.LoadNewCallsAllocation(GenerateCallsAllocation(periodSec, minTalkTimePeriod, maxTalkTimePeriod, callsCountIn15m));
                                break;
                        }
                    }

                    //Если в текущую секунду должен поступить звонок
                    if (skill.CallAllocation.Item1.Contains(currentTime - (periodRequestData - periodSec)))
                    {
                        skill.statistic.CallsOffered++;

                        //Ищем свободного оператора
                        Operator oper = GetFreeOperator(skill.Operators);

                        //Добавляем новый звонок в очередь на скилле
                        if (oper == null)
                            skill.CallsInQueue.Add(new Call(random.Next(1, 9999), skill.CallAllocation.Item2[skill.CallAllocation.Item1.IndexOf(currentTime - (periodRequestData - periodSec))]));
                        else
                        {
                            //Добавляем звонок оператору
                            oper.call = new Call(random.Next(1, 9999), skill.CallAllocation.Item2[skill.CallAllocation.Item1.IndexOf(currentTime - (periodRequestData - periodSec))]);

                            //Меняем статусы оператора
                            oper.CurrentStatus = "Talking";
                            oper.TimeInCurrentStatus = 0;

                            //Добавляем звонок в активные по скиллу
                            skill.ActiveCalls.Add(oper.call);

                            skill.statistic.CallAnswered++;

                            SetOperStatusInAllSkills(skills, oper);
                        }
                    }
                }

                Console.Clear();
                PrintStatistics(currentTime, skills);

                await Task.Delay(1000);
                currentTime++;
            }
        }

        private static void CheckQueueInSkills(List<Skill> skills)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].CallsInQueue.Count > 0 && GetFreeOperator(skills[i].Operators) != null)
                {
                    Operator freeOper = GetFreeOperator(skills[i].Operators);
                    PassCallFromQueueToOperator(skills[i], freeOper);
                    SetOperStatusInAllSkills(skills, freeOper);
                    return;
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

                    currentSkill.ActiveCalls.Remove(currentSkill.ActiveCalls[j]);
                }
            }

            for (int j = 0; j < currentSkill.CallsInQueue.Count; j++)
            {
                currentSkill.CallsInQueue[j].Duration++;

                //Завершаем вызов, если он по длительности больше чем заданная длительность вызова
                if (currentSkill.CallsInQueue[j].Duration > currentSkill.CallsInQueue[j].TalkFullDuration)
                {
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

        static void PrintStatistics(int currentTime, List<Skill> skills)
        {
            Console.WriteLine($"Current time {currentTime} seconds");
            Console.WriteLine($"--------------------------------------");

            foreach (var skill in skills)
            {
                Console.WriteLine($"-= Statistics in skill {skill.SkillName} =-");
                Console.WriteLine($"Offered calls \t\t{skill.statistic.CallsOffered}");
                Console.WriteLine($"Abandoned calls \t{skill.statistic.AbandonedCalls}");
                Console.WriteLine($"Calls answered \t\t{skill.statistic.CallAnswered}");
                Console.WriteLine($"--------------------------------------");

                Console.WriteLine($"Current calls in skill \t\t{skill.CallsInQueue.Count() + skill.ActiveCalls.Count()}");

                Console.WriteLine($"Available operators \t\t{skill.Operators.Where(o => o.CurrentStatus == "Ready").Count()}");
                Console.WriteLine($"Operator talking in skill \t{skill.ActiveCalls.Count()}");
                Console.WriteLine($"Queue in skill \t\t\t{ skill.CallsInQueue.Count() }\t{ GetBar(skill.CallsInQueue.Count(), "*") }");
                Console.WriteLine($"--------------------------------------");

                foreach (var oper in skill.Operators)
                {
                    if (oper.CurrentStatus == "Talking" && oper.call != null)
                        Console.WriteLine($"Operator {oper.Name} in status {oper.CurrentStatus} in skill {skill.SkillName} time = {oper.TimeInCurrentStatus}");
                    else
                    if (oper.CurrentStatus == "Talking" && oper.call == null)
                    {
                        foreach (var skillSearch in skills)
                        {
                            if (skillSearch.SkillName != skill.SkillName)
                                foreach (var operSearch in skillSearch.Operators)
                                {
                                    if (operSearch.call != null && operSearch.Name == oper.Name)
                                        Console.WriteLine($"Operator {oper.Name} in status {oper.CurrentStatus} in skill {skillSearch.SkillName} time = {oper.TimeInCurrentStatus}");
                                }
                        }
                    }
                    else
                        Console.WriteLine($"Operator {oper.Name} in status {oper.CurrentStatus} time = {oper.TimeInCurrentStatus}");
                }

                Console.WriteLine($"--------------------------------------");
            }
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

        static void PassCallFromQueueToOperator(Skill skill, Operator oper)
        { 
            //Получаем данный звонок из очереди
            Call callInQueue = skill.CallsInQueue.OrderBy(c => c.Duration).FirstOrDefault();

            if (callInQueue != null)
            {
                //Если есть свободные операторы, то в цикле выделяем операторов под данные вызовы в очереди
                //Извлечь данный вызов из очереди
                skill.CallsInQueue.Remove(callInQueue);

                //Добавить его в активные вызовы
                callInQueue.Duration = 0;
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

        public Call(int callID, int fullCallDuration)
        { 
            CallID = callID;
            TalkFullDuration = fullCallDuration;
            Duration = 0;
        }
    }

    public class Statistics
    {
        public int CallsOffered { get; set; }
        public int AbandonedCalls { get; set; }
        public int CallAnswered { get; set; }

        public Statistics()
        {
            CallsOffered = 0;
            AbandonedCalls = 0;
            CallAnswered = 0;
        }
    }
}
