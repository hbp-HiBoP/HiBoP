using System;
using System.Collections.Generic;

namespace Tools.CSharp
{
    public static class StringExtension
    {
        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", "partLength");

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        public static void StandardizeToPath(ref string path)
        {
            path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
            path = path.Replace('\\', System.IO.Path.DirectorySeparatorChar);
        }
    }

}
