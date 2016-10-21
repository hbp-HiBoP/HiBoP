using UnityEngine;
using System;
using System.Linq;

namespace Tools.CSharp
{
    public static class ArrayExtension
    {
        public static int Median(this int[] array)
        {
            // Create a clone of the array and sort it.
            int[] l_array = array.Clone() as int[];
            Array.Sort(l_array);

            int l_median = 0;
            if (l_array.Length != 0)
            {
                if (l_array.Length % 2 == 0)
                {
                    //Pair
                    l_median = (l_array[(l_array.Length / 2) - 1] + l_array[(l_array.Length / 2)]) / 2;
                }
                else
                {
                    // Impair
                    l_median = l_array[l_array.Length / 2];
                }
            }
            return l_median;
        }
        public static float Median(this float[] array)
        {
            // Create a clone of the array and sort it.
            float[] l_array = array.Clone() as float[];
            Array.Sort(l_array);

            float l_median = 0;
            if (l_array.Length != 0)
            {
                if (l_array.Length % 2 == 0)
                {
                    //Pair
                    l_median = (l_array[(l_array.Length / 2) - 1] + l_array[(l_array.Length / 2)]) / 2;
                }
                else
                {
                    // Impair
                    l_median = l_array[l_array.Length / 2];
                }
            }
            return l_median;
        }
        public static Vector2 Quantile(this float[] floats, float percentage)
        {
            if ((percentage > 1) || (percentage < 0))
            {
                return Vector2.zero;
            }
            else
            {
                int l_length = floats.Length;
                float l_minRatio = (1 - percentage) / 2.0f;
                float l_maxRatio = l_minRatio + percentage;
                int minValue = Mathf.CeilToInt(l_length * l_minRatio);
                int maxValue = Mathf.CeilToInt(l_length * l_maxRatio);
                return new Vector2(floats[minValue], floats[maxValue - 1]);
            }
        }
        public static string ToDisplay(this float[] floats)
        {
            string l_string = string.Empty;
            l_string += "(";
            foreach (float value in floats)
            {
                l_string += (value.ToString() + ",");
            }
            l_string = l_string.TrimEnd(new char[] { ',' });
            l_string += ")";
            return l_string;
        }
        public static string ToDisplay(this int[] ints)
        {
            string l_string = string.Empty;
            l_string += "(";
            foreach (int value in ints)
            {
                l_string += (value.ToString() + ",");
            }
            l_string = l_string.TrimEnd(new char[] { ',' });
            l_string += ")";
            return l_string;
        }
        /// <summary>
        /// Return the limits of the specified vectors in a Vector4(xMin,xMax,Ymin,YMax).
        /// </summary>
        /// <param name="vectors">Vectors.</param>
        public static Vector4 Limits(this Vector2[] vectors)
        {
            float l_xMin = vectors[0].x;
            float l_xMax = vectors[0].x;
            float l_yMin = vectors[0].y;
            float l_yMax = vectors[0].y;
            foreach (Vector2 vector in vectors)
            {
                if (l_xMin > vector.x)
                {
                    l_xMin = vector.x;
                }
                if (l_yMin > vector.y)
                {
                    l_yMin = vector.y;
                }
                if (l_xMax < vector.x)
                {
                    l_xMax = vector.x;
                }
                if (l_yMax < vector.y)
                {
                    l_yMax = vector.y;
                }
            }
            return new Vector4(l_xMin, l_xMax, l_yMin, l_yMax);
        }
    }
}

