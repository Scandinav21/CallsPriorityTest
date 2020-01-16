using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallFlowModel
{
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
        public int CallsAllocationInterval { get; set; }
        public Dictionary<int, int> CallsDurationAllocation { get; set; }
        public int MinTalkTimeDur { get; set; }
        public int MaxTalkTimeDur { get; set; }
        public PriorityConditions PriorCondition { get; set; }
        public string PriorityConditionString { get; set; }

        public Skill(string name, List<Operator> opers, int priority, (List<int>, List<int>) callAlloc) : this()
        {
            SkillName = name;
            Operators = opers;
            Priority = priority;
            CallAllocation = callAlloc;
        }

        public Skill()
        {
            SkillName = "New skill";
            Priority = 5;
            CallsAllocationInterval = 900;
            MinTalkTimeDur = 10;
            MaxTalkTimeDur = 900;

            if (ActiveCalls == null)
                ActiveCalls = new List<Call>();

            if (CallsInQueue == null)
                CallsInQueue = new List<Call>();

            if (statistic == null)
                statistic = new Statistics();

            if (HistoricalCalls == null)
                HistoricalCalls = new List<Call>();

            if (CallsDurationAllocation == null)
                CallsDurationAllocation = new Dictionary<int, int>
                {
                    { 0, 0 },
                    { 60, 0 },
                    { 120, 0 },
                    { 180, 0 },
                    { 240, 0 },
                    { 300, 0 },
                    { 360, 0 },
                    { 420, 0 },
                    { 480, 0 },
                    { 540, 0 },
                    { 600, 0 }
                };

            if (PriorCondition == null)
                PriorCondition = new PriorityConditions();
        }
    }
}
