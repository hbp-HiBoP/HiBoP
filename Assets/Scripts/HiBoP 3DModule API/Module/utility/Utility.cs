


/**
 * \file    Utility.cs
 * \author  Lance Florian
 * \date    01/01/2016
 * \brief   Define ArrayExtensions and CoroutineUtil
 */


// system
using System;
using System.Collections;

// unity
using UnityEngine;


public static class CoroutineUtil
{
    public static IEnumerator WaitForRealSeconds(float time)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < start + time)
        {
            yield return null;
        }
    }
}

namespace Extensions
{
    public static class ArrayExtensions
    {
        // Inspired from several Stack Overflow discussions and an implementation by David Walker at http://coding.grax.com/2011/11/initialize-array-to-value-in-c-very.html
        public static void Fill<T>(this T[] destinationArray, params T[] value)
        {
            if (destinationArray.Length == 0)
                return;

            if (destinationArray == null)
            {
                throw new ArgumentNullException("destinationArray");
            }

            if (value.Length > destinationArray.Length)
            {
                throw new ArgumentException("Length of value array must not be more than length of destination " + value.Length + " " + destinationArray.Length);
            }

            // set the initial array value
            Array.Copy(value, destinationArray, value.Length);

            int copyLength, nextCopyLength;

            for (copyLength = value.Length; (nextCopyLength = copyLength << 1) < destinationArray.Length; copyLength = nextCopyLength)
            {
                Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);
            }

            Array.Copy(destinationArray, 0, destinationArray, copyLength, destinationArray.Length - copyLength);
        }
    }
}
