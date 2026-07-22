using System;
using System.Buffers;
using System.Collections.Generic;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Core.Data
{
    public struct RunningStatistics
    {
        private double m_Mean;
        private double m_SquaredDistanceSum;

        public long Count { get; private set; }
        public float Mean => Count == 0 ? 0f : (float)m_Mean;
        public float StandardDeviation => Count <= 1 ? 0f : (float)Math.Sqrt(m_SquaredDistanceSum / (Count - 1));
        public float StandardError => Count == 0 ? 0f : StandardDeviation / (float)Math.Sqrt(Count);

        public void Add(float value)
        {
            Count++;
            double delta = value - m_Mean;
            m_Mean += delta / Count;
            double deltaAfterMean = value - m_Mean;
            m_SquaredDistanceSum += delta * deltaAfterMean;
        }

        public void Add(IEnumerable<float> values)
        {
            foreach (float value in values)
                Add(value);
        }
    }

    public static class StreamingStatistics
    {
        public static Vector2 CalculateValueLimit(IEnumerable<float[]> series, float zScore = 1.959964f)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));

            RunningStatistics statistics = new();
            foreach (float[] values in series)
            {
                if (values == null)
                    continue;
                for (int i = 0; i < values.Length; i++)
                    statistics.Add(values[i]);
            }
            return CalculateValueLimit(statistics, zScore);
        }

        public static Vector2 CalculateValueLimit(RunningStatistics statistics, float zScore = 1.959964f)
        {
            if (statistics.Count == 0)
                return Vector2.zero;

            float standardDeviation = Mathf.Abs(statistics.StandardDeviation);
            if (float.IsNaN(standardDeviation) || float.IsInfinity(standardDeviation))
                standardDeviation = 0;
            float offset = zScore * standardDeviation;
            if (offset == 0)
                offset = 1;
            return new Vector2(statistics.Mean - offset, statistics.Mean + offset);
        }

        public static void Calculate(
            IReadOnlyList<float[]> series,
            AveragingType averaging,
            out float[] values,
            out float[] standardErrors)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));
            if (series.Count == 0)
            {
                values = Array.Empty<float>();
                standardErrors = Array.Empty<float>();
                return;
            }

            int sampleCount = series[0].Length;
            for (int i = 1; i < series.Count; ++i)
            {
                if (series[i].Length != sampleCount)
                    throw new ArgumentException("All series must have the same number of samples.", nameof(series));
            }
            values = new float[sampleCount];
            standardErrors = new float[sampleCount];
            float[] medianBuffer = averaging == AveragingType.Median
                ? ArrayPool<float>.Shared.Rent(series.Count)
                : null;
            try
            {
                for (int sample = 0; sample < sampleCount; ++sample)
                {
                    RunningStatistics statistics = new();
                    for (int seriesIndex = 0; seriesIndex < series.Count; ++seriesIndex)
                    {
                        float value = series[seriesIndex][sample];
                        statistics.Add(value);
                        if (medianBuffer != null)
                            medianBuffer[seriesIndex] = value;
                    }

                    values[sample] = averaging == AveragingType.Median
                        ? Median(medianBuffer, series.Count)
                        : statistics.Mean;
                    standardErrors[sample] = statistics.StandardError;
                }
            }
            finally
            {
                if (medianBuffer != null)
                    ArrayPool<float>.Shared.Return(medianBuffer);
            }
        }

        public static float Median(float[] buffer, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (count <= 0 || count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            int middle = count / 2;
            float upper = Select(buffer, count, middle);
            if (count % 2 != 0)
                return upper;

            float lower = buffer[0];
            for (int i = 1; i < middle; ++i)
            {
                if (buffer[i].CompareTo(lower) > 0)
                    lower = buffer[i];
            }
            return (lower + upper) * 0.5f;
        }

        private static float Select(float[] buffer, int count, int target)
        {
            int left = 0;
            int right = count - 1;
            while (left < right)
            {
                int pivot = Partition(buffer, left, right, left + ((right - left) >> 1));
                if (pivot == target)
                    break;
                if (target < pivot) right = pivot - 1;
                else left = pivot + 1;
            }
            return buffer[target];
        }

        private static int Partition(float[] buffer, int left, int right, int pivotIndex)
        {
            float pivot = buffer[pivotIndex];
            Swap(buffer, pivotIndex, right);
            int store = left;
            for (int i = left; i < right; ++i)
            {
                if (buffer[i].CompareTo(pivot) <= 0)
                    Swap(buffer, store++, i);
            }
            Swap(buffer, store, right);
            return store;
        }

        private static void Swap(float[] buffer, int left, int right)
        {
            if (left == right) return;
            (buffer[left], buffer[right]) = (buffer[right], buffer[left]);
        }

        public static int Median(int[] buffer, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (count <= 0 || count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            Array.Sort(buffer, 0, count);
            int middle = count / 2;
            return count % 2 == 0
                ? (buffer[middle - 1] + buffer[middle]) / 2
                : buffer[middle];
        }
    }
}
