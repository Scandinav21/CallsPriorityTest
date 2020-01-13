using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallFlowModel
{
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
