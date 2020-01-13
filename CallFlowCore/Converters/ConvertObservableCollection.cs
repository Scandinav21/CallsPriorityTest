using CallFlowModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CallFlowCore.Converters
{
    public static class ConvertObservableCollection
    {
        public static ObservableCollection<Skill> ToObservableCollection(List<Skill> skills)
        {
            ObservableCollection<Skill> newCollection = new ObservableCollection<Skill>();

            foreach (var skill in skills)
            {
                newCollection.Add(skill);
            }

            return newCollection;
        }
    }
}
