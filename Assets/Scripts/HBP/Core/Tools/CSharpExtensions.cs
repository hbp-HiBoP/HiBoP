using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HBP.Core.Tools
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

    public static class ListExtension
    {
        public static bool AddIfAbsent<T>(this List<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
                return true;
            }
            return false;
        }
    }

    public static class DictionaryExtension
    {
        public static bool AddIfAbsent<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }
            return false;
        }
    }

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
        public static string StandardizeToPath(this string path)
        {
            path = new Regex("/+").Replace(path, "/");
            path = new Regex("\\\\+").Replace(path, "\\");
            path = path.Replace('/', Path.DirectorySeparatorChar);
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            if (path.StartsWith("\\")) path = "\\" + path;
            return path;
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
        }
        public static string ToTooltip(this IEnumerable<string> values, int max)
        {
            string[] array = values.ToArray();
            StringBuilder stringBuilder = new StringBuilder();
            if(array.Length > 0)
            {
                if (array.Length > max)
                {
                    for (int i = 0; i < max - 1; i++)
                    {
                        stringBuilder.AppendLine(string.Format("  • {0}", array[i]));
                    }
                    stringBuilder.AppendLine("  • [...]");
                    stringBuilder.Append(string.Format("  • {0}", array.Last()));
                }
                else
                {
                    for (int i = 0; i < array.Length - 1; i++)
                    {
                        stringBuilder.AppendLine(string.Format("  • {0}", array[i]));
                    }
                    stringBuilder.Append(string.Format("  • {0}", array[array.Length - 1]));
                }
            }
            else
            {
                stringBuilder.Append("  • None");
            }
            return stringBuilder.ToString();
        }
    }

    public static class TypeExtension
    {
        public static string GetDisplayName(this Type type)
        {
            object[] displayNameAttributes = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            if (displayNameAttributes.Length > 0)
            {
                return (displayNameAttributes[0] as DisplayNameAttribute).DisplayName;
            }
            else
            {
                return type.Name;
            }
        }
    }

    public static class ArrayExtensions
    {
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

        public static T[] Create<T>(int length, T value)
        {
            T[] result = new T[length];
            for (int i = 0; i < length; ++i)
                result[i] = value;
            return result;
        }
    }

    public static class NumberExtension
    {
        public static bool IsPowerOfTwo(this int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
        public static bool AreMultiples(this List<int> numbers)
        {
            return numbers.Contains(numbers.GCD());
        }
        public static int GCD(this List<int> numbers)
        {
            return numbers.Aggregate(GCD);
        }
        public static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }
        public static bool TryParseFloat(string text, out float result)
        {
            System.Globalization.CultureInfo[] cultures = new System.Globalization.CultureInfo[]
            {
                System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR"),
                System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"),
                System.Globalization.CultureInfo.CreateSpecificCulture("en-US"),
                System.Globalization.CultureInfo.InvariantCulture
            };
            foreach (var culture in cultures)
            {
                try
                {
                    if (float.TryParse(text, System.Globalization.NumberStyles.Float, culture, out result))
                    {
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            result = 0;
            return false;
        }
        public static float ParseFloat(string text)
        {
            if (TryParseFloat(text, out float result))
                return result;
            return 0;
        }
    }

    public static class FileSystemExtensions
    {
        public static void CopyFilesRecursively(this DirectoryInfo source, DirectoryInfo target)
        {
            if (!source.Exists) return;

            if (!target.Exists) Directory.CreateDirectory(target.FullName);

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
        public static string CopyToDirectory(this string path, DirectoryInfo targetDirectory, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(path)) return "";

            FileInfo file = new FileInfo(path);

            if (!file.Exists) return path;
            if (!targetDirectory.Exists) return path;

            string newFilePath = Path.Combine(targetDirectory.FullName, file.Name);
            if (new FileInfo(newFilePath).Exists) return newFilePath;

            File.Copy(file.FullName, newFilePath, overwrite);
            return newFilePath;
        }
    }
}