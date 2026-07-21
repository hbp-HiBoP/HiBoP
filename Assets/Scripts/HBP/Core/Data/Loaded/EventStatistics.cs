using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Core.Data
{
    public struct EventStatistics
    {
        #region Properties
        public int RoundedTimeFromStart { get; set; }
        public float TimeFromStart { get; set; }
        public float NumberOfOccurenceBySubTrial { get; set; }
        public int NumberOfOccurences { get; set; }
        #endregion

        #region Constructors
        public EventStatistics(EventInformation[] eventInformation, Enums.AveragingType averaging) : this()
        {
            if (!eventInformation.Any(e => e.IsFound))
                return;

            int occurrenceCount = eventInformation.Sum(info => info.Occurences.Length);
            RunningStatistics timeStatistics = new();
            int totalOccurrencesBySubTrial = 0;
            float[] times = averaging == Enums.AveragingType.Median
                ? ArrayPool<float>.Shared.Rent(occurrenceCount)
                : null;
            int[] occurrencesBySubTrial = averaging == Enums.AveragingType.Median
                ? ArrayPool<int>.Shared.Rent(eventInformation.Length)
                : null;
            int timeIndex = 0;
            int subTrialIndex = 0;
            try
            {
                foreach (EventInformation eventInfo in eventInformation)
                {
                    int subTrialOccurrenceCount = eventInfo.Occurences.Length;
                    totalOccurrencesBySubTrial += subTrialOccurrenceCount;
                    if (occurrencesBySubTrial != null)
                        occurrencesBySubTrial[subTrialIndex++] = subTrialOccurrenceCount;

                    foreach (EventInformation.EventOccurence occurrence in eventInfo.Occurences)
                    {
                        NumberOfOccurences++;
                        timeStatistics.Add(occurrence.TimeFromStart);
                        if (times != null)
                            times[timeIndex++] = occurrence.TimeFromStart;
                    }
                }

                switch (averaging)
                {
                    case Enums.AveragingType.Mean:
                        TimeFromStart = timeStatistics.Mean;
                        NumberOfOccurenceBySubTrial = totalOccurrencesBySubTrial / eventInformation.Length;
                        break;
                    case Enums.AveragingType.Median:
                        TimeFromStart = StreamingStatistics.Median(times, occurrenceCount);
                        NumberOfOccurenceBySubTrial = StreamingStatistics.Median(occurrencesBySubTrial, eventInformation.Length);
                        break;
                }
                RoundedTimeFromStart = Mathf.RoundToInt(TimeFromStart);
            }
            finally
            {
                if (times != null)
                    ArrayPool<float>.Shared.Return(times);
                if (occurrencesBySubTrial != null)
                    ArrayPool<int>.Shared.Return(occurrencesBySubTrial);
            }
        }
        #endregion

        #region Public Methods
        public static EventStatistics Average(IEnumerable<EventStatistics> eventStatistics)
        {
            EventStatistics result = new();
            foreach (EventStatistics eventStat in eventStatistics)
            {
                result.TimeFromStart += eventStat.TimeFromStart * eventStat.NumberOfOccurences;
                result.NumberOfOccurenceBySubTrial += eventStat.NumberOfOccurenceBySubTrial * eventStat.NumberOfOccurences;
                result.NumberOfOccurences += eventStat.NumberOfOccurences;
            }
            if (result.NumberOfOccurences > 0)
            {
                result.TimeFromStart /= result.NumberOfOccurences;
                result.NumberOfOccurenceBySubTrial /= result.NumberOfOccurences;
                result.RoundedTimeFromStart = Mathf.RoundToInt(result.TimeFromStart);
            }
            else
            {
                result.TimeFromStart = 0;
                result.RoundedTimeFromStart = 0;
                result.NumberOfOccurenceBySubTrial = 0;
                result.NumberOfOccurences = 0;
            }
            return result;
        }
        #endregion
    }
}
