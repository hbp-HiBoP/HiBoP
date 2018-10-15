using HBP.Data.Experience.Dataset;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public struct EventInformationStat
    {
        #region Properties
        public int RoundedIndex { get; set; }
        public float Index { get; set; }
        public float NumberOfOccurenceBySubTrial { get; set; }
        public int NumberOfOccurences { get; set; }
        #endregion

        #region Constructors
        public EventInformationStat(EventInformation[] eventInformation, Enums.AveragingType averaging):this()
        {
            List<int> indexes = new List<int>();
            List<int> numberOfOccurence = new List<int>();
            foreach (var eventInfo in eventInformation)
            {
                numberOfOccurence.Add(eventInfo.Occurences.Length);
                foreach (var occurence in eventInfo.Occurences)
                {
                    NumberOfOccurences++;
                    indexes.Add(occurence.Index);
                }
            }
            switch (averaging)
            {
                case Enums.AveragingType.Mean:
                    Index = indexes.ToArray().Mean();
                    NumberOfOccurenceBySubTrial = numberOfOccurence.ToArray().Mean();
                    RoundedIndex = Mathf.RoundToInt(Index);
                    break;
                case Enums.AveragingType.Median:
                    Index = indexes.ToArray().Median();
                    NumberOfOccurenceBySubTrial = numberOfOccurence.ToArray().Median();
                    RoundedIndex = Mathf.RoundToInt(Index);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}