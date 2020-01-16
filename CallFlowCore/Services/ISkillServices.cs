using CallFlowModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CallFlowCore.Services
{
    public interface ISkillServices
    {
        List<Operator> GenerateOperators(int countOfOperators, int startIndex);

        (List<int>, List<int>) GenerateCallsAllocation(Dictionary<int, int> callDurationAllocation, int intervalSeconds, int minTalkTimePer, int maxTalkTimePer);

        void AnswerCall(Operator oper, List<Skill> skills, Skill currentSkill, int currentTime);

        void PutCallToQueue(List<Skill> skills, Skill skill, int currentTime);

        void CheckQueueInSkills(List<Skill> skills, int currentTime);

        int UpdateSkillData(List<Skill> skills, Skill currentSkill);

        Operator GetFreeOperator(List<Operator> opers);

        void TryRaiseCallPriority(Skill currentSkill, int maxWaitTimeBeforeRaisePrior, int raisedPrior);

        string GetStatistics(int currentTime, List<Skill> skills, Skill loadStatisticsFromSkill = null, bool showBrief = false, bool showOperStat = false);

        Skill ResetSkill(Skill skill);

        ObservableCollection<Skill> ResetSkills(ObservableCollection<Skill> skills);

        PriorityConditions GetPriorityConditions(string query);

        void TryRaiseSkillPriority(ObservableCollection<Skill> skills, Skill currentSkill);
    }
}
