using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            path = path.Replace('\\', '/');
        }
        public static string SplitPascalCase(this string pascalCase)
        {
            Regex r = new Regex("([A-Z]+[a-z]+)");
            string result = r.Replace(pascalCase, m => m.Value.ToLower() + " ").Trim();
            result = char.ToUpper(result[0]) + result.Substring(1);
            return result;
        }
        public static string CamelCaseToWords(this string camelCase)
        {
            return Regex.Replace(camelCase, @"\B[A-Z][a-z]", m => " " + m.ToString().ToLower());
            //return Regex.Replace(camelCase, @"\B[A-Z]", m => " " + m.ToString().ToLower());
        }
    }

}
