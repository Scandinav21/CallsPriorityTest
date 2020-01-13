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
}
