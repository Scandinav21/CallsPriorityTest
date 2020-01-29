using CallFlowModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CallFlowCore.Services
{
    public interface ISkillServices
    {
        List<Operator> GenerateOperators(int countOfOperators, int startIndex);

        Task<(List<int>, List<int>)> GenerateCallsAllocation(Dictionary<int, int> callDurationAllocation, int startAllocationTime, int intervalSeconds, int maxTalkTimePer);

        void AnswerCall(Operator oper, List<Skill> skills, Skill currentSkill, int currentTime);

        void PutCallToQueue(List<Skill> skills, Skill skill, int currentTime);

        void CheckQueueInSkills(List<Skill> skills, int currentTime);

        int UpdateSkillData(List<Skill> skills, Skill currentSkill);

        Operator GetFreeOperator(List<Operator> opers);

        bool TryRaiseCallPriority(Skill currentSkill, int maxWaitTimeBeforeRaisePrior, int raisedPrior);

        string GetStatistics(int currentTime, List<Skill> skills, Skill loadStatisticsFromSkill = null, bool showBrief = false, bool showOperStat = false);

        Task<Skill> ResetSkill(Skill skill);

        Task<ObservableCollection<Skill>> ResetSkills(ObservableCollection<Skill> skills);

        PriorityConditions GetPriorityConditions(string query);

        bool TryRaiseSkillPriority(ObservableCollection<Skill> skills, Skill currentSkill);

        Dictionary<string, int> GetOperatorsDict(List<Operator> opers, string status = null);
    }
}
