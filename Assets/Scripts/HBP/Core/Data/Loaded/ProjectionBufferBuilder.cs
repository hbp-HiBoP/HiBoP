using System;
using System.Collections.Generic;

namespace HBP.Core.Data
{
    public static class ProjectionBufferBuilder
    {
        public static float[] FlattenTimeMajor(
            IReadOnlyList<float[]> valuesBySite,
            IReadOnlyList<bool> maskedSites,
            int timelineLength,
            out RunningStatistics unmaskedStatistics,
            out float minimum,
            out float maximum)
        {
            if (valuesBySite == null)
                throw new ArgumentNullException(nameof(valuesBySite));
            if (maskedSites == null)
                throw new ArgumentNullException(nameof(maskedSites));
            if (valuesBySite.Count != maskedSites.Count)
                throw new ArgumentException("The mask count must match the site count.", nameof(maskedSites));
            if (timelineLength < 0)
                throw new ArgumentOutOfRangeException(nameof(timelineLength));

            float[] flattened = new float[checked(timelineLength * valuesBySite.Count)];
            unmaskedStatistics = new RunningStatistics();
            minimum = float.MaxValue;
            maximum = float.MinValue;
            for (int site = 0; site < valuesBySite.Count; ++site)
            {
                float[] values = valuesBySite[site];
                if (values == null || values.Length != timelineLength)
                    throw new ArgumentException("Every site series must match the projection timeline length.", nameof(valuesBySite));

                bool masked = maskedSites[site];
                for (int time = 0; time < timelineLength; ++time)
                {
                    float value = values[time];
                    flattened[time * valuesBySite.Count + site] = value;
                    if (masked)
                        continue;
                    unmaskedStatistics.Add(value);
                    if (value < minimum)
                        minimum = value;
                    if (value > maximum)
                        maximum = value;
                }
            }

            if (unmaskedStatistics.Count == 0)
            {
                minimum = -1f;
                maximum = 1f;
            }
            return flattened;
        }
    }
}
