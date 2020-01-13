using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallFlowModel
{
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
}
