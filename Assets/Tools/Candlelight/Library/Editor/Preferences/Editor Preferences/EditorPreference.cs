// 
// EditorPreference.cs
// 
// Copyright (c) 2012-2015, Candlelight Interactive, LLC
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
// 
// This file contains a class for working with editor preferences.

using UnityEditor;
using System.IO;
using System.Xml.Serialization;

namespace Candlelight
{
	/// <summary>
	/// Editor preference.
	/// </summary>
	/// <remarks>
	/// This class exists primarily to bypass problems with initializing values from EditorPrefs from a constructor.
	/// </remarks>
	public class EditorPreference<T, TEditor>
	{
		/// <summary>
		/// The default value.
		/// </summary>
		private readonly T m_DefaultValue;
		/// <summary>
		/// Flag specifying whether the value has yet been initialized from preferences.
		/// </summary>
		private bool m_IsValueInitializedFromPrefs = false;

		#region Backing Fields
		private T m_CurrentValue;
		#endregion
		/// <summary>
		/// Gets or sets the current value.
		/// </summary>
		/// <value>The current value. </value>
		public T CurrentValue
		{
			get { return GetCurrentValue(); }
			set { SetCurrentValue(value); }
		}
		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		/// <value>The name of the property.</value>
		public string PropertyName { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EditorPreference{T, TEditor}"/> class.
		/// </summary>
		/// <param name="variableName">Variable name.</param>
		/// <param name="defaultValue">Default value.</param>
		public EditorPreference(string variableName, T defaultValue)
		{
			this.PropertyName = GetPreferenceKey(typeof(TEditor), variableName);
			m_DefaultValue = defaultValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EditorPreference{T, TEditor}"/> class for a foldout
		/// state.
		/// </summary>
		/// <returns>An EditorPreference for a foldout state.</returns>
		/// <param name="variableName">Variable name.</param>
		/// <param name="defaultValue">If set to <see langword="true"/> default value.</param>
		/// <typeparam name="T">The preference type.</typeparam>
		/// <typeparam name="TEditor">The editor type.</typeparam>
		public static EditorPreference<bool, TEditor> ForFoldoutState(string variableName, bool defaultValue)
		{
			return new EditorPreference<bool, TEditor>(GetFoldoutStatePrefKeyName(variableName), defaultValue);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EditorPreference{T, TEditor}"/> class for a toggle.
		/// </summary>
		/// <returns>An EditorPreference for a toggle.</returns>
		/// <param name="variableName">Variable name.</param>
		/// <param name="defaultValue">If set to <see langword="true"/> default value.</param>
		/// <typeparam name="T">The preference type.</typeparam>
		/// <typeparam name="TEditor">The editor type.</typeparam>
		public static EditorPreference<bool, TEditor> ForToggle(string variableName, bool defaultValue)
		{
			return new EditorPreference<bool, TEditor>(GetTogglePrefKeyName(variableName), defaultValue);
		}
		
		/// <summary>
		/// Get a preference key in the form of ClassName - varName.
		/// </summary>
		/// <returns>The preference key.</returns>
		/// <param name="cls">Class to which the key belongs.</param>
		/// <param name="variableName">Variable name.</param>
		private static string GetPreferenceKey(System.Type cls, string variableName)
		{
			return string.Format("{0} - {1}", cls, variableName);
		}
		
		/// <summary>
		/// Gets the name of the foldout state preference key.
		/// </summary>
		/// <returns>
		/// The foldout state preference key name.
		/// </returns>
		/// <param name="foldoutName">Foldout name.</param>
		private static string GetFoldoutStatePrefKeyName(string foldoutName)
		{
			return string.IsNullOrEmpty(foldoutName) ?
				"isFoldoutExpanded" :
				string.Format("is{0}{1}FoldoutExpanded", foldoutName[0].ToString().ToUpper(), foldoutName.Substring(1));
		}
		
		/// <summary>
		/// Gets the name of the toggle preference key.
		/// </summary>
		/// <returns>The toggle preference key name.</returns>
		/// <param name="toggleName">Toggle name.</param>
		private static string GetTogglePrefKeyName(string toggleName)
		{
			return string.IsNullOrEmpty(toggleName) ?
				"isEnabled" :
				string.Format("is{0}{1}Enabled", toggleName[0].ToString().ToUpper(), toggleName.Substring(1));
		}
		
		/// <summary>
		/// Gets the XML string.
		/// </summary>
		/// <returns>The XML string.</returns>
		/// <param name="value">Value.</param>
		private string GetXMLString(object obj)
		{
			XmlSerializer xml = new XmlSerializer(obj.GetType());
			StringWriter sw = new StringWriter();
			xml.Serialize(sw, obj);
			sw.Close();
			return sw.ToString();
		}
		
		/// <summary>
		/// Gets the current value.
		/// </summary>
		/// <returns>The current value.</returns>
		/// <exception cref='System.ArgumentException'>Is thrown when the current value cannot be deserialized.
		/// </exception>
		private T GetCurrentValue()
		{
			if (!m_IsValueInitializedFromPrefs)
			{
				System.Type t = typeof(T);
				if (t == typeof(bool))
				{
					m_CurrentValue = (T)(object)EditorPrefs.GetBool(this.PropertyName, (bool)(object)m_DefaultValue);
				}
				else if (t == typeof(float))
				{
					m_CurrentValue = (T)(object)EditorPrefs.GetFloat(this.PropertyName, (float)(object)m_DefaultValue);
				}
				else if (t == typeof(int) || t.IsEnum)
				{
					m_CurrentValue = (T)(object)EditorPrefs.GetInt(this.PropertyName, (int)(object)m_DefaultValue);
				}
				else if (t == typeof(string))
				{
					m_CurrentValue = (T)(object)EditorPrefs.GetString(this.PropertyName, m_DefaultValue as string);
				}
				else
				{
					try
					{
						m_CurrentValue =
							DeserializeObject(EditorPrefs.GetString(this.PropertyName, ""), m_DefaultValue);
					}
					catch (System.Exception)
					{
						throw new System.ArgumentException("Unable to deserialize object from preferences.");
					}
				}
				m_IsValueInitializedFromPrefs = true;
			}
			return m_CurrentValue;
		}
		
		/// <summary>
		/// Sets the current value.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <exception cref='System.ArgumentException'>Is thrown when the value cannot be serialized.</exception>
		private void SetCurrentValue(T value)
		{
			// early out if already initialized and no change
			if (m_IsValueInitializedFromPrefs && m_CurrentValue.Equals(value))
			{
				return;
			}
			System.Type t = typeof(T);
			if (t == typeof(bool))
			{
				EditorPrefs.SetBool(this.PropertyName, (bool)(object)value);
			}
			else if (t == typeof(float))
			{
				EditorPrefs.SetFloat(this.PropertyName, (float)(object)value);
			}
			else if (t == typeof(int) || t.IsEnum)
			{
				EditorPrefs.SetInt(this.PropertyName, (int)(object)value);
			}
			else if (t == typeof(string))
			{
				EditorPrefs.SetString(this.PropertyName, value as string);
			}
			else
			{
				try
				{
					EditorPrefs.SetString(this.PropertyName, GetXMLString(value));
				}
				catch (System.Exception)
				{
					throw new System.ArgumentException("Unable to serialize object to preferences.");
				}
			}
			m_CurrentValue = value;
		}
		
		/// <summary>
		/// Deserialize the object represented by XML string.
		/// </summary>
		/// <returns>The object.</returns>
		/// <param name="xmlString">XML string.</param>
		/// <param name="defaultValue">Default value.</param>
		private T DeserializeObject(string xmlString, T defaultValue)
		{
			XmlSerializer xml = new XmlSerializer(typeof(T));
			StringReader sr = new StringReader(xmlString);
			T value;
			try
			{
				value = (T)xml.Deserialize(sr);
			}
			catch (System.Exception)
			{
				value = defaultValue;
			}
			finally
			{
				sr.Close();
			}
			return value;
		}
	}
}