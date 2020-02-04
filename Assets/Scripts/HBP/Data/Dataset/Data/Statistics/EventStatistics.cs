﻿using System.Collections.Generic;
using UnityEngine;
using Tools.CSharp;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public struct EventStatistics
    {
        #region Properties
        public int RoundedIndexFromStart { get; set; }
        public float IndexFromStart { get; set; }
        public int RoundedTimeFromStart { get; set; }
        public float TimeFromStart { get; set; }
        public float NumberOfOccurenceBySubTrial { get; set; }
        public int NumberOfOccurences { get; set; }
        #endregion

        #region Constructors
        public EventStatistics(EventInformation[] eventInformation, Enums.AveragingType averaging):this()
        {
            if (eventInformation.Any(e => e.IsFound))
            {
                List<int> indexes = new List<int>();
                List<float> times = new List<float>();
                List<int> numberOfOccurence = new List<int>();
                foreach (var eventInfo in eventInformation)
                {
                    numberOfOccurence.Add(eventInfo.Occurences.Length);
                    foreach (var occurence in eventInfo.Occurences)
                    {
                        NumberOfOccurences++;
                        indexes.Add(occurence.IndexFromStart);
                        times.Add(occurence.TimeFromStart);
                    }
                }
                switch (averaging)
                {
                    case Enums.AveragingType.Mean:
                        IndexFromStart = indexes.ToArray().Mean();
                        TimeFromStart = times.ToArray().Mean();
                        NumberOfOccurenceBySubTrial = numberOfOccurence.ToArray().Mean();
                        RoundedIndexFromStart = Mathf.RoundToInt(IndexFromStart);
                        RoundedTimeFromStart = Mathf.RoundToInt(TimeFromStart);
                        break;
                    case Enums.AveragingType.Median:
                        IndexFromStart = indexes.ToArray().Median();
                        TimeFromStart = times.ToArray().Median();
                        NumberOfOccurenceBySubTrial = numberOfOccurence.ToArray().Median();
                        RoundedIndexFromStart = Mathf.RoundToInt(IndexFromStart);
                        RoundedTimeFromStart = Mathf.RoundToInt(TimeFromStart);
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Public Methods
        public static EventStatistics Average(IEnumerable<EventStatistics> eventStatistics)
        {
            EventStatistics result = new EventStatistics();
            foreach (var eventStat in eventStatistics)
            {
                result.IndexFromStart += eventStat.IndexFromStart * eventStat.NumberOfOccurences;
                result.TimeFromStart += eventStat.TimeFromStart * eventStat.NumberOfOccurences;
                result.NumberOfOccurenceBySubTrial += eventStat.NumberOfOccurenceBySubTrial * eventStat.NumberOfOccurences;
                result.NumberOfOccurences += eventStat.NumberOfOccurences;
            }
            if (result.NumberOfOccurences > 0)
            {
                result.IndexFromStart /= result.NumberOfOccurences;
                result.TimeFromStart /= result.NumberOfOccurences;
                result.NumberOfOccurenceBySubTrial /= result.NumberOfOccurences;
                result.RoundedIndexFromStart = Mathf.RoundToInt(result.IndexFromStart);
                result.RoundedTimeFromStart = Mathf.RoundToInt(result.TimeFromStart);
            }
            else
            {
                result.IndexFromStart = 0;
                result.TimeFromStart = 0;
                result.RoundedIndexFromStart = 0;
                result.RoundedTimeFromStart = 0;
                result.NumberOfOccurenceBySubTrial = 0;
                result.NumberOfOccurences = 0;
            }
            return result;
        }
        #endregion
    }
}