using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallFlowModel
{
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
}
