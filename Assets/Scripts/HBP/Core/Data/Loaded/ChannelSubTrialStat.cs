using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Enums;

namespace HBP.Core.Data
{
    /// <summary>
    /// Statistical data for one sub-trial and one channel.
    /// </summary>
    public struct ChannelSubTrialStat
    {
        #region Properties

        public float[] Values { get; set; }
        public float[] SEM { get; set; }
        public int TotalNumberOfSubTrials { get; set; }
        public int NumberOfFoundSubTrials { get; set; }

        #endregion

        #region Constructors

        public ChannelSubTrialStat(float[] values, float[] sem) : this()
        {
            Values = values;
            SEM = sem;
        }

        public ChannelSubTrialStat(ChannelSubTrial[] subTrials, bool[] isValid, AveragingType averaging)
        {
            if (subTrials == null)
                throw new ArgumentNullException(nameof(subTrials));
            if (isValid == null)
                throw new ArgumentNullException(nameof(isValid));
            if (subTrials.Length != isValid.Length)
                throw new ArgumentException("Validity flags must match the sub-trial count.", nameof(isValid));

            TotalNumberOfSubTrials = subTrials.Length;
            NumberOfFoundSubTrials = subTrials.Count(subTrial => subTrial.Found);
            List<float[]> validSeries = new(isValid.Count(valid => valid));
            for (int trialIndex = 0; trialIndex < subTrials.Length; ++trialIndex)
            {
                if (isValid[trialIndex])
                    validSeries.Add(subTrials[trialIndex].Values);
            }

            StreamingStatistics.Calculate(validSeries, averaging, out float[] values, out float[] sem);
            Values = values;
            SEM = sem;
        }

        #endregion

        #region Public Methods

        public void Clear()
        {
            Values = Array.Empty<float>();
            SEM = Array.Empty<float>();
            TotalNumberOfSubTrials = 0;
            NumberOfFoundSubTrials = 0;
        }

        #endregion
    }
}
