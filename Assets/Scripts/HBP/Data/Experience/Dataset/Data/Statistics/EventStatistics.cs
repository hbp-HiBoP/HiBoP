using System.Collections.Generic;
using UnityEngine;
using Tools.CSharp;

namespace HBP.Data.Experience.Dataset
{
    public struct EventStatistics
    {
        #region Properties
        public int RoundedIndexFromStart { get; set; }
        public float IndexFromStart { get; set; }
        public float NumberOfOccurenceBySubTrial { get; set; }
        public int NumberOfOccurences { get; set; }
        #endregion

        #region Constructors
        public EventStatistics(EventInformation[] eventInformation, Enums.AveragingType averaging):this()
        {
            List<int> indexes = new List<int>();
            List<int> numberOfOccurence = new List<int>();
            foreach (var eventInfo in eventInformation)
            {
                numberOfOccurence.Add(eventInfo.Occurences.Length);
                foreach (var occurence in eventInfo.Occurences)
                {
                    NumberOfOccurences++;
                    indexes.Add(occurence.IndexFromStart);
                }
            }
            switch (averaging)
            {
                case Enums.AveragingType.Mean:
                    IndexFromStart = indexes.ToArray().Mean();
                    NumberOfOccurenceBySubTrial = numberOfOccurence.ToArray().Mean();
                    RoundedIndexFromStart = Mathf.RoundToInt(IndexFromStart);
                    break;
                case Enums.AveragingType.Median:
                    IndexFromStart = indexes.ToArray().Median();
                    NumberOfOccurenceBySubTrial = numberOfOccurence.ToArray().Median();
                    RoundedIndexFromStart = Mathf.RoundToInt(IndexFromStart);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Public Methods
        public static EventStatistics Average(IEnumerable<EventStatistics> eventStatistics)
        {
            EventStatistics result = new EventStatistics();
            foreach (var eventStat in eventStatistics)
            {
                result.IndexFromStart = eventStat.IndexFromStart * eventStat.NumberOfOccurences;
                result.NumberOfOccurenceBySubTrial = eventStat.NumberOfOccurenceBySubTrial * eventStat.NumberOfOccurences;
                result.NumberOfOccurences += eventStat.NumberOfOccurences;
            }
            result.IndexFromStart /= result.NumberOfOccurences;
            result.NumberOfOccurenceBySubTrial /= result.NumberOfOccurences;
            result.RoundedIndexFromStart = Mathf.RoundToInt(result.IndexFromStart);
            return result;
        }
        #endregion
    }
}