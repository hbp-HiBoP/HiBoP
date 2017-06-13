using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Tools.CSharp
{
    public static class MathfExtension
    {
        public static int GreatestCommonDivisor(int a, int b)
        {
            int Remainder;

            while (b != 0)
            {
                Remainder = a % b;
                a = b;
                b = Remainder;
            }

            return a;
        }
        public static float Average(float[] array)
        {
            float l_result = 0;
            foreach(float i_v in array)
            {
                l_result += i_v;
            }
            l_result /= array.Length;
            return l_result;
        }
        public static float StandardDeviation(float[] array)
        {
            if(array.Length > 1)
            {
                float l_average = Average(array);
                float l_sum = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    l_sum += Mathf.Pow((array[i] - l_average), 2);
                }
                float l_result = l_sum * (1.0f / (array.Length - 1));
                l_result = Mathf.Sqrt(l_result);
                return l_result;
            }
            else
            {
                return 0;
            }

        }
        public static float SEM(float[] array)
        {
            return StandardDeviation(array) / Mathf.Sqrt(array.Length);
        }
        public static T Median<T>(this T[] array) where T : IComparable
        {
            Array.Sort(array);
            return array[Mathf.CeilToInt(array.Length / 2)];
        }
        public static T Median<T>(this List<T> array) where T : IComparable
        {
            array.Sort();
            return array[Mathf.CeilToInt(array.Count / 2)];
        }

    }
}