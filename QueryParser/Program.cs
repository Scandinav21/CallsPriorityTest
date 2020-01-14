using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QueryParser
{
    class Program
    {
        static void Main(string[] args)
        {
            //string query = "if queue in each (skill1,skill2,skill3,skill4) > 50 then priority = 4 and timeWait > 3 then priority = 2";
            //string query = "if timeWait > 1 then priority = 1 and queue in (skill1,skill2,skill3,skill4) > 10 then priority = 3";
            //string query = "if timeWait > 3 then priority = 2";
            //string query = "if queue in (skill1,skill2,skill3,skill4) > 50 then priority = 4";
            string query = "if queue in each (skill1,skill2) > 50 then priority = 4 or timeWait > 3 then priority = 2";

            bool queueSingle = false;
            bool queueMultiple = false;
            bool timeWait = false;
            string signTimeWait = "";
            string signQueue = "";
            int queueVal = 0;
            int timeWaitVal = 0;
            int priorityWhenQ = 0;
            int priorityWhenTimeW = 0;
            //1 and 
            //-1 or
            int unitCondition = 0;
            List<string> skills = new List<string>();

            string regexp;

            //Проверяем есть ли условие по очереди на скиллах
            if ((bool)CheckRegexIsMatch(query, @"queue \w+"))
            {
                //Проверяем очередь только на одном из скиллов
                if((bool)CheckRegexIsMatch(query, @"queue in \(\w+"))
                    queueSingle = true;

                //Проверяем очередь на всех скиллах списка
                if ((bool)CheckRegexIsMatch(query, @"queue in each \(\w+"))
                    queueMultiple = true;

                //Получаем список скиллов
                regexp = @"\((\w+\,{0,})+\)";
                if ((bool)CheckRegexIsMatch(query, regexp))
                {
                    string skillsStr = (string)CheckRegexIsMatch(query, regexp, true);
                    skills = ParseSkills(skillsStr, ',', new char[] { '(', ')'});
                }

                //Получаем знак сравнения для очереди
                regexp = @"\) <|>|<=|>=|== \d+";
                if ((bool)CheckRegexIsMatch(query, regexp))
                    signQueue = (string)CheckRegexIsMatch(query, regexp, true);

                //Получаем значение очереди
                regexp = @"\) .{1,2} \d+";
                if ((bool)CheckRegexIsMatch(query, regexp))
                {
                    queueVal = GetIntAfterSign(query, regexp);
                }

                regexp = @"queue.*\) (<|>|<=|>=|=) \d+ then priority = \d+";
                if ((bool)CheckRegexIsMatch(query, regexp))
                {
                    priorityWhenQ = GetIntAfterSign(query, regexp);
                }
            }

            if ((bool)CheckRegexIsMatch(query, @"timewait > \d+"))
            {
                timeWait = true;

                regexp = @"timewait <|>|<=|>=|== \d+";
                if ((bool)CheckRegexIsMatch(query, regexp))
                    signTimeWait = (string)CheckRegexIsMatch(query, regexp, true);

                regexp = @"timewait .{1,2} \d+";
                if ((bool)CheckRegexIsMatch(query, regexp))
                {
                    timeWaitVal = GetIntAfterSign(query, regexp);
                }

                regexp = @"timeWait.(<|>|<=|>=|=) \d+ then priority = \d+";
                if ((bool)CheckRegexIsMatch(query, regexp))
                {
                    priorityWhenTimeW = GetIntAfterSign(query, regexp);
                }
            }

            regexp = @" and ";
            if ((bool)CheckRegexIsMatch(query, regexp))
                unitCondition = 1;

            regexp = @" or ";
            if ((bool)CheckRegexIsMatch(query, regexp))
                unitCondition = -1;

            if (!queueSingle && queueMultiple && timeWait && signTimeWait == ">" && signQueue == ">" && queueVal == 50 &&
                timeWaitVal == 3 && priorityWhenQ == 4 && skills.Count == 2 && unitCondition == -1 && priorityWhenTimeW == 2
                )
            {
                Console.WriteLine("Success");

                foreach (var skill in skills)
                {
                    Console.WriteLine(skill);
                }
            }

            Console.ReadLine();
        }

        public static object CheckRegexIsMatch(string str, string regexStr, bool getMatchVal = false)
        {
            Regex regex = new Regex(regexStr, RegexOptions.IgnoreCase);

            if (!getMatchVal)
                return regex.IsMatch(str);
            else
                if (getMatchVal)
                return regex.Match(str).Value;
            else
                return false;
        }

        public static int GetIntAfterSign(string str, string regexStr)
        {
            int result = -1;

            Regex regex = new Regex(regexStr, RegexOptions.IgnoreCase);

            string tempVal = regex.Match(str).Value;
            IEnumerable<char> tempValArr = tempVal.Reverse();
            tempVal = "";

            foreach (var item in tempValArr)
            {
                tempVal += item.ToString();
            }

            regex = new Regex(@"^\d+");
            tempValArr = regex.Match(tempVal).Value.Reverse();

            tempVal = "";

            foreach (var item in tempValArr)
            {
                tempVal += item.ToString();
            }

            Int32.TryParse(tempVal, out result);


            return result;
        }

        public static List<string> ParseSkills(string skills, char separator, char[] curves)
        {
            List<string> skillsList = new List<string>();

            skills = skills.Trim(curves[0]);
            skills = skills.Trim(curves[1]);

            string[] skillsSplitted = skills.Split(separator);

            foreach (var skill in skillsSplitted)
            {
                skillsList.Add(skill);
            }

            return skillsList;
        }
    }
}
