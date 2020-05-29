using System.Runtime.InteropServices;
using UnityEngine;

namespace Tools.CSharp
{
    public static class MathDLL
    {
        public static Vector2 CalculateValueLimit(this float[] array, float Zscore = 1.959964f)
        {
            if(array == null || array.Length == 0)
            {
                return new Vector2(0, 0);
            }
            else
            {
                float absStandardDeviation = Mathf.Abs(array.StandardDeviation());
                float mean = array.Mean();
                float offset = Zscore * absStandardDeviation;
                if (offset == 0) offset = 1;
                return new Vector2(mean - offset, mean + offset);
            }
        }
        public static float StandardDeviation(this float[] array)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            return StandardDeviation(array, array.Length);
        }
        public static float SEM(this float[] array)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            return SEM(array, array.Length);
        }
        public static float Mean(this float[] array)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            return Mean(array, array.Length);
        }
        public static float Mean(this int[] array)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            return Mean(array, array.Length);
        }
        public static float Median(this float[] array)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            return Median(array, array.Length);
        }
        public static int Median(this int[] array)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            return Median(array, array.Length);
        }
        public static void Normalize(this float[] array, float[] targetArray, float average, float standardDeviation)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            if (Mathf.Approximately(standardDeviation, 0)) standardDeviation = 1;
            Normalize(array, array.Length, targetArray, average, standardDeviation);
        }
        public static float Lerp(float value1, float value2, float percentage)
        {
            return LerpDLL(value1, value2, percentage);
        }
        public static float[] LinearSmooth(this float[] values, int smoothFactor)
        {
            float[] newValues = new float[(values.Length - 1) * smoothFactor + 1];
            LinearSmooth(values, values.Length, smoothFactor, newValues);
            return newValues;
        }
        public static float[] Interpolate(this float[] values, int size, int before, int after)
        {
            if (size == values.Length || values.Length == 0) return values.Clone() as float[];
            
            float[] newValues = new float[size];
            Interpolate(values, values.Length, newValues, size, before, after);
            return newValues;
        }
        public static float Pearson(float[] baseline, float[] values)
        {
            return PearsonCorrelationCoefficient(baseline, baseline.Length, values, values.Length);
        }
        public static float WilcoxonRankSum(float[] values1, float[] values2)
        {
            return WilcoxonRankSum(values1, values1.Length, values2, values2.Length);
        }
        public static float WilcoxonSignedRank(float[] values1, float[] values2)
        {
            return WilcoxonSignedRank(values1, values1.Length, values2, values2.Length);
        }

        #region DLL
        [DllImport("hbp_math", EntryPoint = "MeanFloat", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float Mean(float[] values, int lenght);
        [DllImport("hbp_math", EntryPoint = "MeanInt", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Mean(int[] values, int lenght);
        [DllImport("hbp_math", EntryPoint = "MedianFloat", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float Median(float[] values, int lenght);
        [DllImport("hbp_math", EntryPoint = "MedianInt", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Median(int[] values, int lenght);
        [DllImport("hbp_math", EntryPoint = "StandardDeviation", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float StandardDeviation(float[] values, int lenght);
        [DllImport("hbp_math", EntryPoint = "SEM", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float SEM(float[] values, int lenght);
        [DllImport("hbp_math", EntryPoint = "Normalize", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void Normalize(float[] values, int length, float[] targetArray, float average, float standardDeviation);
        [DllImport("hbp_math", EntryPoint = "Lerp", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float LerpDLL(float value1, float value2, float percentage);
        [DllImport("hbp_math", EntryPoint = "LinearSmooth", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void LinearSmooth(float[] values, int length, int smoothFactor, float[] newValues);
        [DllImport("hbp_math", EntryPoint = "Interpolate", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void Interpolate(float[] values, int length, float[] newValues, int newLength, int before, int after);
        [DllImport("hbp_math", EntryPoint = "PearsonCorrelationCoefficient", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float PearsonCorrelationCoefficient(float[] baseline, int baselineLength, float[] values, int valuesLength);
        [DllImport("hbp_math", EntryPoint = "WilcoxonRankSum", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float WilcoxonRankSum(float[] values1, int length1, float[] values2, int length2);
        [DllImport("hbp_math", EntryPoint = "WilcoxonSignedRank", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float WilcoxonSignedRank(float[] values1, int length1, float[] values2, int length2);
        #endregion
    }
}