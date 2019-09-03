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

        #region DLL
        [DllImport("HBP_Compute", EntryPoint = "MeanFloat", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float Mean(float[] values, int lenght);
        [DllImport("HBP_Compute", EntryPoint = "MeanInt", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Mean(int[] values, int lenght);
        [DllImport("HBP_Compute", EntryPoint = "MedianFloat", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float Median(float[] values, int lenght);
        [DllImport("HBP_Compute", EntryPoint = "MedianInt", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Median(int[] values, int lenght);
        [DllImport("HBP_Compute", EntryPoint = "StandardDeviation", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float StandardDeviation(float[] values, int lenght);
        [DllImport("HBP_Compute", EntryPoint = "SEM", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float SEM(float[] values, int lenght);
        [DllImport("HBP_Compute", EntryPoint = "Normalize", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void Normalize(float[] values, int length, float[] targetArray, float average, float standardDeviation);
        [DllImport("HBP_Compute", EntryPoint = "Lerp", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float LerpDLL(float value1, float value2, float percentage);
        [DllImport("HBP_Compute", EntryPoint = "LinearSmooth", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void LinearSmooth(float[] values, int length, int smoothFactor, float[] newValues);
        [DllImport("HBP_Compute", EntryPoint = "Interpolate", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void Interpolate(float[] values, int length, float[] newValues, int newLength, int before, int after);
        #endregion
    }
}