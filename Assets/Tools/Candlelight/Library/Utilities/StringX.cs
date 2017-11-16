// 
// StringX.cs
// 
// Copyright (c) 2011-2017, Candlelight Interactive, LLC
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE. 

using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Candlelight
{
	/// <summary>
	/// An extension class for <see cref="System.String"/> objects.
	/// </summary>
	public static class StringX
	{
		#region Delegates
		private delegate bool ParseEnumCallback(string value, out object parsedValue);
		#endregion

		/// <summary>
		/// A simple disposable struct that can be used in conjunction with the "using" keyword to retrieve a pooled
		/// <see cref="StringBuilder"/>.
		/// </summary>
		public struct StringBuilderScope : System.IDisposable
		{
			#region Backing Fields
			private StringBuilder m_StringBuilder;
			#endregion

			/// <summary>
			/// Gets the string builder.
			/// </summary>
			/// <value>The string builder.</value>
			public StringBuilder StringBuilder
			{
				get { return m_StringBuilder = m_StringBuilder ?? s_StringBuilderPool.Get(); }
			}

			/// <summary>
			/// Releases all resource used by the <see cref="StringX.StringBuilderScope"/> object.
			/// </summary>
			/// <remarks>
			/// Call <see cref="Dispose"/> when you are finished using the <see cref="StringX.StringBuilderScope"/>.
			/// The <see cref="Dispose"/> method leaves the <see cref="StringX.StringBuilderScope"/> in an unusable
			/// state. After calling <see cref="Dispose"/>, you must release all references to the
			/// <see cref="StringX.StringBuilderScope"/> so the garbage collector can reclaim the memory that the
			/// <see cref="StringX.StringBuilderScope"/> was occupying.
			/// </remarks>
			public void Dispose()
			{
				if (m_StringBuilder != null)
				{
					s_StringBuilderPool.Release(m_StringBuilder);
				}
			}
		}

		/// <summary>
		/// Methods for parsing a decimal string into an enum based on its underlying type.
		/// </summary>
		private static readonly Dictionary<System.Type, ParseEnumCallback> s_EnumParsers =
			new Dictionary<System.Type, ParseEnumCallback>()
		{
			{
				typeof(byte),
				delegate(string s, out object parsedValue)
				{
					byte parsed; bool result = byte.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(int),
				delegate(string s, out object parsedValue)
				{
					int parsed; bool result = int.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(long),
				delegate(string s, out object parsedValue)
				{
					long parsed; bool result = long.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(sbyte),
				delegate(string s, out object parsedValue)
				{
					sbyte parsed; bool result = sbyte.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(short),
				delegate(string s, out object parsedValue)
				{
					short parsed; bool result = short.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(uint),
				delegate(string s, out object parsedValue)
				{
					uint parsed; bool result = uint.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(ulong),
				delegate(string s, out object parsedValue)
				{
					ulong parsed; bool result = ulong.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
			{
				typeof(ushort),
				delegate(string s, out object parsedValue)
				{
					ushort parsed; bool result = ushort.TryParse(s, out parsed); parsedValue = parsed; return result;
				}
			},
		};
		/// <summary>
		/// A regular expression to extract s base name, used to auto increment names.
		/// </summary>
		private static readonly Regex s_MatchBaseName = new Regex(@"(?<name>.*?)(?<number>\d*$)");
		/// <summary>
		/// The index of the "name" match group.
		/// </summary>
		private static readonly int s_MatchBaseNameNameGroup = 1;
		/// <summary>
		/// The index of the "number" match group.
		/// </summary>
		private static readonly int s_MatchBaseNameNumberGroup = 2;
		/// <summary>
		/// A <see cref="ObjectPool{T}"/> of <see cref="System.Text.StringBuilder"/> objects to reuse as needed.
		/// </summary>
		private static readonly ObjectPool<StringBuilder> s_StringBuilderPool =
			new ObjectPool<StringBuilder>(null, sb => sb.Remove(0, sb.Length));
#pragma warning disable 414
		/// <summary>
		/// The title case text info.
		/// </summary>
		private static readonly TextInfo s_TitleCaseTextInfo = new CultureInfo("en-US").TextInfo;
#pragma warning restore 414

		/// <summary>
		/// Determines whether or not the specified <see cref="string"/> contains any lowercase characters.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified <see cref="string"/> contains any lowercase characters; otherwise,
		/// <see langword="false"/>.
		/// </returns>
		/// <param name="str">String.</param>
		public static bool ContainsLowercase(this string str)
		{
			for (int i = 0; i < str.Length; ++i)
			{
				if (char.IsLower(str[i]))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines whether or not the specified <see cref="string"/> contains any uppercase characters.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified <see cref="string"/> contains any uppercase characters; otherwise,
		/// <see langword="false"/>.
		/// </returns>
		/// <param name="str">String.</param>
		public static bool ContainsUppercase(this string str)
		{
			for (int i = 0; i < str.Length; ++i)
			{
				if (char.IsUpper(str[i]))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets an auto-incremented version of the specified base name that does not already exist in the specified
		/// collection.
		/// </summary>
		/// <returns>An auto-incremented version of the specified base name that does not already exist in the specified
		/// collection.</returns>
		/// <param name="baseName">Base name.</param>
		/// <param name="existingNames">Existing names.</param>
		public static string GetUniqueName(this string baseName, IList<string> existingNames)
		{
			using (var allNames = new HashPool<string>.Scope())
			{
				if (existingNames != null)
				{
					for (int i = 0; i < existingNames.Count; ++i)
					{
						allNames.HashSet.Add(existingNames[i]);
					}
				}
				return GetUniqueName(baseName, allNames.HashSet);
			}
		}

		/// <summary>
		/// Gets an auto-incremented version of the specified base name that does not already exist in the specified
		/// collection.
		/// </summary>
		/// <returns>An auto-incremented version of the specified base name that does not already exist in the specified
		/// collection.</returns>
		/// <param name="baseName">Base name.</param>
		/// <param name="existingNames">Existing names.</param>
		public static string GetUniqueName(this string baseName, HashSet<string> existingNames)
		{
			if (existingNames == null || existingNames.Count == 0 || !existingNames.Contains(baseName))
			{
				return baseName;
			}
			int startIndex = 1;
			string newName = baseName;
			if (s_MatchBaseName.IsMatch(baseName))
			{
				baseName = s_MatchBaseName.Match(baseName).Groups[s_MatchBaseNameNameGroup].Value;
				if (
					int.TryParse(
						s_MatchBaseName.Match(baseName).Groups[s_MatchBaseNameNumberGroup].Value, out startIndex
					)
				)
				{
					++startIndex;
				}
				else
				{
					startIndex = 1;
				}
			}
			for (int i = startIndex; i >= 0; ++i)
			{
				newName = string.Format("{0}{1}", baseName, i);
				if (!existingNames.Contains(newName))
				{
					break;
				}
			}
			return newName;
		}

		/// <summary>
		/// Return a string which is the concatenation of the strings in the collection. The separator between elements
		/// is the string providing this method. Mimics str.join(iterable) in Python.
		/// </summary>
		/// <returns>The sequence of <see cref="System.String"/>s concatenated with the delimiter.</returns>
		/// <param name="separator">Separator that should join strings in the collection.</param>
		/// <param name="strings">Collection of strings.</param>
		public static string Join(this string separator, IEnumerable<string> strings)
		{
			return string.Join(separator, strings.ToArray());
		}

		/// <summary>
		/// Gets a slice of the supplied <see cref="System.String"/> using the specified start index, end index, and
		/// skip. Mimics slicing operator in Python.
		/// </summary>
		/// <returns>A slice of the supplied <see cref="System.String"/>.</returns>
		/// <param name="str">String to slice.</param>
		/// <param name="start">Start index.</param>
		/// <param name="end">End index.</param>
		/// <param name="skip">Number of indices to skip with each step.</param>
		public static string Range(this string str, int start, int end = -1, int skip = 1)
		{
			end = end >= 0 ? Mathf.Min(end, str.Length) : str.Length + end;
			using (var characters = new ListPool<char>.Scope())
			{
				for (int index = start; index < end; index += skip)
				{
					characters.List.Add(str[index]);
				}
				return new string(characters.List.ToArray());
			}
		}
		
		/// <summary>
		/// Converts <see cref="System.Single"/> meters value to feet and inches string.
		/// </summary>
		/// <returns>A feet/inches string representation of the supplied meters value.</returns>
		/// <param name="meters">Meters.</param>
		public static string ToFeetInchesString(this float meters)
		{
			float inches = meters * 39.37007874015748f; // 100f / 2.54f
			int feet = (int)(inches * 0.083333333333333f); // 1f / 12f
			inches = inches - feet * 12;
			return string.Format("{0}' {1:0}\"", feet, inches);
		}

		/// <summary>
		/// Gets the hex string representation of <paramref name="color"/> in the form #XXXXXXXX.
		/// </summary>
		/// <returns>The hex string representation of <paramref name="color"/> in the form #XXXXXXXX.</returns>
		/// <param name="color">Color.</param>
		public static string ToHexString(this Color color)
		{
			return string.Format(
				"#{0}{1}{2}{3}",
				((int)(color.r * 255f)).ToString("X2"),
				((int)(color.g * 255f)).ToString("X2"),
				((int)(color.b * 255f)).ToString("X2"),
				((int)(color.a * 255f)).ToString("X2")
			);
		}

		/// <summary>
		/// Converts the specified camelCase or PascalCase string to snake_case. For example, "localPosition" or
		/// "LocalPosition" becomes "local_position".
		/// </summary>
		/// <returns>
		/// The supplied camelCase or PascalCase string converted into underscore-delimited, lowercased words.
		/// </returns>
		/// <param name="compoundWord">A string in camelCase or PascalCase.</param>
		public static string ToSnakeCase(this string compoundWord)
		{
			string result = Regex.Replace(compoundWord, @"(\B[A-Z])", m => string.Format("_{0}", m.Value.ToLower()));
			return string.Format("{0}{1}", result[0].ToString().ToLower(), result.Substring((1)));
		}
		
		/// <summary>
		/// Converts <see cref="System.Single"/> to a time string.
		/// </summary>
		/// <returns>The time string representation of the supplied time value.</returns>
		/// <param name="time">Time in seconds.</param>
		public static string ToTimeString(this float time)
		{
			return string.Format("{0}:{1:00.00}", (int)(time % 3600)/60, time % 60);
		}

		/// <summary>
		/// Converts the words to title case words. For example, "local position" becomes "Local Position".
		/// </summary>
		/// <returns>The supplied words converted to title case.</returns>
		/// <param name="words">A string containing one or more words.</param>
		public static string ToTitleCase(this string words)
		{
#if !(UNITY_WINRT || UNITY_WINRT_8_0 || UNITY_WINRT_8_1)
			return s_TitleCaseTextInfo.ToTitleCase(words);
#else
			if (words == null)
			{
				return null;
			}
			if (words.Length == 0)
			{
				return words;
			}
			StringBuilder sb = s_StringBuilderPool.Get();
			sb.Append(words);
			sb[0] = char.ToUpper(sb[0]);
			for (int i = 1; i < sb.Length; ++i)
			{
				if (char.IsWhiteSpace(sb[i - 1]))
				{
					sb[i] = char.ToUpper(sb[i]);
				}
				else
				{
					sb[i] = char.ToLower(sb[i]);
				}
			}
			string result = sb.ToString();
			s_StringBuilderPool.Release(sb);
			return result;
#endif
		}

		/// <summary>
		/// Converts the specified camelCase, PascalCase, or snake_case string to words. For example, "localPosition",
		/// "LocalPosition", and "local_position" become "Local Position".
		/// </summary>
		/// <returns>
		/// The supplied camelCase, PascalCase, or snake_case string broken into individual, capitalized words.
		/// </returns>
		/// <param name="compoundWord">A string in camelCase, PascalCase, or snake_case.</param>
		public static string ToWords(this string compoundWord)
		{
			return Regex.Replace(compoundWord, @"(_(?<word>[A-Za-z]))|(\B(?<word>[A-Z]))", " ${word}").ToTitleCase();
		}

		/// <summary>
		/// Tries to parse the specified <paramref name="numberString"/> into an enum value of the specified
		/// <paramref name="enumType"/>.
		/// </summary>
		/// <returns>
		/// <see langword="true"/>, if <paramref name="numberString"/> was successfully parsed; otherwise,
		/// <see langword="false"/>.
		/// </returns>
		/// <param name="enumType">A <see cref="System.Enum"/> type.</param>
		/// <param name="numberString">
		/// A <see cref="System.String"/> that has serialized an enum value using a decimal number.
		/// </param>
		/// <param name="enumValue">
		/// <paramref name="numberString"/> as <paramref name="enumType"/> if successful; otherwise, the default value
		/// of <paramref name="enumType"/>.
		/// </param>
		public static bool TryParseNumberStringAsEnum(System.Type enumType, string numberString, out object enumValue)
		{
			if (string.IsNullOrEmpty(numberString))
			{
				enumValue = System.Activator.CreateInstance(enumType);
				return false;
			}
			enumValue = null;
			System.Type underlyingType = System.Enum.GetUnderlyingType(enumType);
			if (
				!s_EnumParsers[underlyingType](numberString, out enumValue) ||
				!System.Enum.IsDefined(enumType, enumValue)
			)
			{
				enumValue = System.Activator.CreateInstance(enumType);
				return false;
			}
			enumValue = System.Enum.ToObject(enumType, enumValue);
			return true;
		}
	}
}