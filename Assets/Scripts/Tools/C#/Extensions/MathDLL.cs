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
        #endregion
    }
}