using CallFlowModel;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallFlowCore.Messages
{
    public class NewSkillMessage : PubSubEvent<Skill>
    {
    }
}
