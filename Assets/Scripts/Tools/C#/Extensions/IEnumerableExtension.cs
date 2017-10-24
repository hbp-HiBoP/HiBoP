using System.Collections.Generic;
using System;
using System.Text;
using System.Linq;

namespace Tools.CSharp
{
    public static class IEnumerableExtension
    {
        public static string ToDisplay<T>(this IEnumerable<T> IEnumerable)
        {
            StringBuilder stringBuilder = new StringBuilder("(");
            foreach (var item in IEnumerable)
            {
                stringBuilder.Append(item.ToString() + ",");
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }
        public static IEnumerable<T> DeepClone<T>(this IEnumerable<T> IEnumerable) where T : ICloneable
        {
            return IEnumerable.Select(a => (T)a.Clone());
        }
    } 
}

