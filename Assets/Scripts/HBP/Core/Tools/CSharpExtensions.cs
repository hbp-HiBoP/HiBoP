using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace HBP.Core.Tools
{
    public static class IEnumerableExtension
    {
        public static string ToDisplay<T>(this IEnumerable<T> IEnumerable)
        {
            StringBuilder stringBuilder = new("(");
            foreach (var item in IEnumerable)
            {
                stringBuilder.Append(item.ToString() + ",");
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }
        public static IEnumerable<T> DeepClone<T>(this IEnumerable<T> IEnumerable, bool forceEnumeration = false) where T : ICloneable
        {
            if (forceEnumeration)
            {
                return IEnumerable.Select(a => (T)a.Clone()).ToList();
            }
            else
            {
                return IEnumerable.Select(a => (T)a.Clone());
            }
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
            Regex r = new("([A-Z]+[a-z]+)");
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
            StringBuilder stringBuilder = new();
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
        public static string GenerateUniqueFilePath(this string path)
        {
            string result = path;
            string extension = Path.GetExtension(result);
            string pathWithoutExtension = Path.GetFullPath(result).Remove(Path.GetFullPath(result).Length - extension.Length);
            int count = 0;
            while (File.Exists(result))
            {
                string temp = string.Format("{0}({1})", pathWithoutExtension, ++count);
                result = temp + extension;
            }
            return result;
        }
        public static string GenerateUniqueDirectoryPath(this string path)
        {
            string result = path;
            string fullPath = Path.GetFullPath(result);
            int count = 0;
            while (Directory.Exists(result))
            {
                result = string.Format("{0}({1})", fullPath, ++count);
            }
            return result;
        }
        public static bool IsBIDS(this string path)
        {
            FileInfo participantsFileInfo = new(Path.Combine(path, "participants.tsv"));
            return participantsFileInfo.Exists;
        }
        public static string DeblankCompletely(this string value)
        {
            string deblanked = value.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
            deblanked = Regex.Replace(deblanked, @"\s+", " ").Trim();
            return deblanked;
        }
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Replace spaces, hyphens, and other separators with underscores
            string result = input.Trim();

            // Replace common separators with underscores
            result = Regex.Replace(result, @"[\s\-\.]+", "_");

            // Handle camelCase and PascalCase by adding underscores before uppercase letters
            // that are followed by lowercase letters or preceded by lowercase letters
            result = Regex.Replace(result, @"([a-z])([A-Z])", "$1_$2");
            result = Regex.Replace(result, @"([A-Z])([A-Z][a-z])", "$1_$2");

            // Convert to lowercase
            result = result.ToLowerInvariant();

            // Remove multiple consecutive underscores
            result = Regex.Replace(result, @"_{2,}", "_");

            // Remove leading and trailing underscores
            result = result.Trim('_');

            return result;
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

            FileInfo file = new(path);

            if (!file.Exists) return path;
            if (!targetDirectory.Exists) return path;

            string newFilePath = Path.Combine(targetDirectory.FullName, file.Name);
            if (new FileInfo(newFilePath).Exists) return newFilePath;

            File.Copy(file.FullName, newFilePath, overwrite);
            return newFilePath;
        }
    }

    public static class UniTaskExtensions
    {
        public static async UniTask WhenAllSequenced(IEnumerable<UniTask> tasks)
        {
            foreach (var task in tasks)
                await task;
        }
        public static async UniTask<IEnumerable<T>> WhenAllSequenced<T>(IEnumerable<UniTask<T>> tasks)
        {
            T[] results = new T[tasks.Count()];
            int i = 0;
            foreach (var task in tasks)
                results[i++] = await task;
            return results;
        }
        public static async UniTask PerformMultipleTasksAsync(IEnumerable<Func<UniTask>> tasks, float startProgress, float endProgress, string loadingText, Action<float, float, LoadingText> updateProgress, int maxConcurrency, bool parallel, CancellationToken token = default)
        {
            var taskList = tasks.ToList();
            int count = 0;
            int length = taskList.Count;
            updateProgress.Invoke(startProgress, 0, new LoadingText(loadingText));
            if (parallel)
            {
                if (maxConcurrency <= 0)
                {
                    var tasksToExecute = taskList.Select(async task =>
                    {
                        token.ThrowIfCancellationRequested();
                        await task();
                        lock (updateProgress)
                        {
                            count++;
                            updateProgress.Invoke(startProgress + (float)count / length * (endProgress - startProgress), 0.2f, new LoadingText(loadingText, " ", count + "/" + length));
                        }
                    });
                    await UniTask.WhenAll(tasksToExecute);
                }
                else
                {
                    using var semaphore = new SemaphoreSlim(maxConcurrency);
                    var tasksToExecute = taskList.Select(async task =>
                    {
                        await semaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            await task();
                            lock (updateProgress)
                            {
                                count++;
                                updateProgress.Invoke(startProgress + (float)count / length * (endProgress - startProgress), 0.2f, new LoadingText(loadingText, " ", count + "/" + length));
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await UniTask.WhenAll(tasksToExecute);
                }
            }
            else
            {
                foreach (var task in taskList)
                {
                    token.ThrowIfCancellationRequested();
                    await task();
                    count++;
                    updateProgress.Invoke(startProgress + (float)count / length * (endProgress - startProgress), 0.2f, new LoadingText(loadingText, " ", count + "/" + length));
                }
            }
        }
        public static async UniTask<IEnumerable<T>> PerformMultipleTasksAsync<T>(IEnumerable<Func<UniTask<T>>> tasks, float startProgress, float endProgress, string loadingText, Action<float, float, LoadingText> updateProgress, int maxConcurrency, bool parallel, CancellationToken token = default)
        {
            var taskList = tasks.ToList();
            int count = 0;
            int length = taskList.Count;
            updateProgress.Invoke(startProgress, 0, new LoadingText(loadingText));
            if (parallel)
            {
                if (maxConcurrency == 0)
                {
                    var tasksToExecute = taskList.Select(async task =>
                    {
                        token.ThrowIfCancellationRequested();
                        T data = await task();
                        lock (updateProgress)
                        {
                            count++;
                            updateProgress.Invoke(startProgress + (float)count / length * (endProgress - startProgress), 0.2f, new LoadingText(loadingText, " ", count + "/" + length));
                        }
                        return data;
                    });
                    var result = await UniTask.WhenAll(tasksToExecute);
                    return result;
                }
                else
                {
                    using var semaphore = new SemaphoreSlim(maxConcurrency);
                    T[] results = new T[taskList.Count];
                    var tasksToExecute = taskList.Select(async (task, index) =>
                    {
                        await semaphore.WaitAsync(token);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            results[index] = await task();
                            lock (updateProgress)
                            {
                                count++;
                                updateProgress.Invoke(startProgress + (float)count / length * (endProgress - startProgress), 0.2f, new LoadingText(loadingText, " ", count + "/" + length));
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await UniTask.WhenAll(tasksToExecute);
                    return results;
                }
            }
            else
            {
                List<T> result = new();
                foreach (var task in taskList)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(await task());
                    count++;
                    updateProgress.Invoke(startProgress + (float)count / length * (endProgress - startProgress), 0.2f, new LoadingText(loadingText, " ", count + "/" + length));
                }
                return result;
            }
        }
    }
}
