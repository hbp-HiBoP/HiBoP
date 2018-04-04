using System.Runtime.InteropServices;

namespace Tools.CSharp
{
    public static class MathDLL
    {
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
        public static float[] Normalize(this float[] array, float average, float standardDeviation)
        {
            if (array.Length == 0)
            {
                throw new System.Exception("Array is empty");
            }
            float[] tmparray = (float[])array.Clone();
            if (standardDeviation == 0) standardDeviation = 1;
            Normalize(tmparray, array.Length, average, standardDeviation);
            return tmparray;
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
        private static extern void Normalize(float[] values, int length, float average, float standardDeviation);
        [DllImport("HBP_Compute", EntryPoint = "Lerp", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern float LerpDLL(float value1, float value2, float percentage);
        [DllImport("HBP_Compute", EntryPoint = "LinearSmooth", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void LinearSmooth(float[] values, int length, int smoothFactor, float[] newValues);
        #endregion
    }
}