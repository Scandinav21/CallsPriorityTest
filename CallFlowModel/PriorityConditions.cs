using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallFlowModel
{
    public class PriorityConditions
    {
        public bool queueSingle = false;
        public bool queueMultiple = false;
        public int queueVal = 0;
        public bool timeWait = false;
        public int timeWaitVal = 0;
        public string signTimeWait = "";
        public string signQueue = "";
        public int priorityWhenQ = 0;
        public int priorityWhenTimeW = 0;
        //1 and 
        //-1 or
        public int unitCondition = 0;
        public List<string> skills = new List<string>();
    }
}
