using System;
using System.Linq;

namespace Tools.CSharp
{
    public static class CloneExtension
    {
        public static T[] DeepClone<T>(this T[] arrayToClone) where T : ICloneable
        {
            T[] clone = arrayToClone.Select(a => (T)a.Clone()).ToArray();
            return clone;
        }
    }
}