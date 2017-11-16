// 
// IPropertyBackingFieldCompatible.cs
// 
// Copyright (c) 2014-2016, Candlelight Interactive, LLC
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

using System.Collections.Generic;
using System.Linq;

namespace Candlelight
{
	/// <summary>
	/// An interface to specify that a serializable type is compatible with <see cref="PropertyBackingFieldAttribute"/>.
	/// </summary>
	public interface IPropertyBackingFieldCompatible : System.ICloneable
	{
		/// <summary>
		/// Gets a hash value that is based on the values of the serialized properties of this instance.
		/// </summary>
		/// <remarks>
		/// Note that any reference type fields should implement and test with this interface;
		/// <see cref="System.Collections.IList"/> fields should generate a value-based hash.
		/// </remarks>
		/// <returns>A hash value based on the values of the serialized properties on this instance.</returns>
		int GetSerializedPropertiesHash();
	}

	/// <summary>
	/// A generic interface to specify that a serializable struct is compatible with
	/// <see cref="PropertyBackingFieldAttribute"/>. It exists mainly to add a compile-time reminder to implement more
	/// optimized equality comparison.
	/// </summary>
	/// <typeparam name="T">The type of struct implementing this interface.</typeparam>
	public interface IPropertyBackingFieldCompatible<T> : IPropertyBackingFieldCompatible
#if !CANDLELIGHT_MONO_AOT
		, System.IEquatable<T>
#endif
		where T : struct
	{

	}

	/// <summary>
	/// Backing field utility class.
	/// </summary>
	public static class BackingFieldUtility
	{
		/// <summary>
		/// A mode specifying how duplicate <see cref="System.Int32"/> keys should be handled.
		/// </summary>
		public enum IntKeyMode
		{
			/// <summary>
			/// Later entries of duplicate keys should be incremented until they are unique.
			/// </summary>
			Increment,
			/// <summary>
			/// Layer entries of duplicate keys should be zet to 0. Only the first 0-keyed entry will be retained. Use
			/// this mode for keys that should not logically be incremented, such as hash codes.
			/// </summary>
			SetToZero
		}

		/// <summary>
		/// Converts the string key to lowercase if needed.
		/// </summary>
		/// <returns>The string key to lowercase if needed.</returns>
		/// <param name="stringKey">String key.</param>
		/// <typeparam name="T">The identifier type.</typeparam>
		private static T ConvertStringKeyToLower<T>(T stringKey)
		{
			string key = (string)(object)stringKey;
			return (T)(object)(key.ContainsUppercase() ? key.ToLower() : key);
		}

		/// <summary>
		/// Generates a hash code for the serialized properties in a list or array of
		/// <see cref="IPropertyBackingFieldCompatible"/> objects.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="listField">List field.</param>
		/// <typeparam name="T">The element type.</typeparam>
		public static int GenerateSerializedPropertiesHash<T>(IList<T> listField)
			where T : IPropertyBackingFieldCompatible
		{
			int typeCode = typeof(T).GetHashCode();
			if (listField == null)
			{
				// a null and empty collection are the same where the inspector is concerned
				return ObjectX.GenerateHashCode(typeCode, 0.GetHashCode());
			}
			int result = ObjectX.GenerateHashCode(typeCode, listField.Count.GetHashCode());
			for (int i = 0; i < listField.Count; ++i)
			{
				result = ObjectX.GenerateHashCode(
					result, listField[i] == null ? typeCode : listField[i].GetSerializedPropertiesHash()
				);
			}
			return result;
		}

		/// <summary>
		/// Gets the backing field for a public interface property that also has a proxy backing field to serialize
		/// <see cref="UnityEngine.Object"/>s that implement the interface.
		/// </summary>
		/// <returns>The current value of the <typeparamref name="T"/> backing field.</returns>
		/// <param name="interfaceBackingField">Interface backing field.</param>
		/// <param name="objectBackingField">
		/// Serialized backing field for a <see cref="UnityEngine.Object"/> that implements <typeparamref name="T"/>.
		/// </param>
		/// <typeparam name="T">The interface to which the backing field should be restricted.</typeparam>
		public static T GetInterfaceBackingField<T>(ref T interfaceBackingField, UnityEngine.Object objectBackingField)
			where T : class
		{
			return interfaceBackingField =
				interfaceBackingField == null ? objectBackingField as T : interfaceBackingField;
		}

		/// <summary>
		/// Gets the keyed list backing field as a dictionary.
		/// </summary>
		/// <param name="backingField">Backing field of identifiable objects.</param>
		/// <param name="result">Result.</param>
		/// <param name="getData">Method to get the data of interest from the identifiable object.</param>
		/// <typeparam name="T">An identifiable backing field compatible object wrapper type.</typeparam>
		/// <typeparam name="TId">The identifier type.</typeparam>
		/// <typeparam name="TData">The data type.</typeparam>
		public static void GetKeyedListBackingFieldAsDict<T, TId, TData>(
			List<T> backingField, Dictionary<TId, TData> result, System.Func<T, TData> getData
		) where T : IIdentifiable<TId>
		{
			result.Clear();
			for (int i = 0; i < backingField.Count; ++i)
			{
				result[backingField[i].Identifier] = getData(backingField[i]);
			}
		}

		/// <summary>
		/// Sets a backing field for a set of objects.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field.</param>
		/// <param name="value">Value.</param>
		/// <param name="ignoreCase">
		/// If set to <see langword="true"/>, then all string elements keys will be made lowercase.
		/// </param>
		/// <param name="intKeyMode">Mode for handling duplicate <see cref="System.Int32"/> keys.</param>
		/// <typeparam name="T">
		/// The element type (either <see cref="UnityEngine.Object"/>-derived, <see cref="System.Int32"/>, or
		/// <see cref="System.String"/>).
		/// </typeparam>
		private static bool SetHashedListBackingFieldFromArray<T>(
			List<T> backingField, IList<T> value, bool ignoreCase, IntKeyMode intKeyMode = IntKeyMode.Increment
		)
		{
			if (value == null || value.Count == 0)
			{
				int oldCount = backingField.Count;
				backingField.Clear();
				return oldCount != 0;
			}
			T[] oldValue = backingField.ToArray();
			backingField.Clear();
			bool isString = typeof(T) == typeof(string);
			bool isInt = !isString && typeof(T) == typeof(int);
			if (!isInt && !isString) // NOTE: WinRT cannot use Type.IsAssignableFrom()
			{
				int nullHash = 0;
				int nullIndex = backingField.FindIndex(item => item.GetHashCode() == nullHash);
				foreach (T newValue in value)
				{
					if (
						!object.ReferenceEquals(newValue, default(T)) &&
						backingField.FindIndex(item => item.GetHashCode() == newValue.GetHashCode()) < 0
					)
					{
						backingField.Add(newValue);
						if (newValue.GetHashCode() == nullHash)
						{
							nullIndex = backingField.Count - 1;
						}
					}
					else if (nullIndex < 0)
					{
						backingField.Add(default(T));
						nullIndex = backingField.Count - 1;
					}
				}
			}
			else
			{
				using (var hashedValues = new HashPool<T>.Scope())
				{
					using (var stringKeys = new HashPool<string>.Scope())
					{
						for (int i = 0; i < value.Count; ++i)
						{
							T id = value[i];
							if (isString)
							{
								string key = (string)(object)id ?? string.Empty;
								if (ignoreCase && key.ContainsUppercase())
								{
									key = key.ToLower();
								}
								key = key.GetUniqueName(stringKeys.HashSet);
								stringKeys.HashSet.Add(key);
								id = (T)(object)key;
							}
							else if (isInt)
							{
								switch (intKeyMode)
								{
								case IntKeyMode.Increment:
									while (hashedValues.HashSet.Contains(id))
									{
										id = (T)(object)((int)(object)id + 1);
									}
									break;
								case IntKeyMode.SetToZero:
									if (hashedValues.HashSet.Contains(id))
									{
										id = (T)(object)0;
										if (hashedValues.HashSet.Contains(id))
										{
											continue;
										}
									}
									break;
								}
							}
							hashedValues.HashSet.Add(id);
						}
						foreach (T hashedValue in hashedValues.HashSet)
						{
							backingField.Add(hashedValue);
						}
					}
				}
			}
			return !oldValue.SequenceEqual(backingField);
		}

		/// <summary>
		/// Sets a backing field for a set of <see cref="System.Int32"/>s.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of <see cref="System.Int32"/> elements.</param>
		/// <param name="value">Value.</param>
		/// <param name="mode">Mode.</param>
		public static bool SetHashedListBackingFieldFromIntArray(
			List<int> backingField, IList<int> value, IntKeyMode mode = IntKeyMode.Increment
		)
		{
			return SetHashedListBackingFieldFromArray<int>(backingField, value, false, mode);
		}

		/// <summary>
		/// Sets a backing field for a set of <see cref="UnityEngine.Object"/>s.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of <see cref="UnityEngine.Object"/> elements.</param>
		/// <param name="value">Value.</param>
		/// <typeparam name="T">A <see cref="UnityEngine.Object"/>-derived type.</typeparam>
		public static bool SetHashedListBackingFieldFromObjectArray<T>(List<T> backingField, IList<T> value)
			where T : UnityEngine.Object
		{
			return SetHashedListBackingFieldFromArray<T>(backingField, value, false);
		}

		/// <summary>
		/// Sets a backing field for a set of <see cref="System.String"/>s.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of <see cref="System.String"/> elements.</param>
		/// <param name="value">Value.</param>
		/// <param name="ignoreCase">If set to <see langword="true"/>, then all elements will be made lowercase.</param>
		public static bool SetHashedListBackingFieldFromStringArray(
			List<string> backingField, IList<string> value, bool ignoreCase = false
		)
		{
			return SetHashedListBackingFieldFromArray<string>(backingField, value, ignoreCase);
		}

		/// <summary>
		/// Sets a backing field for a set of objects.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field.</param>
		/// <param name="value">Value.</param>
		/// <param name="ignoreCase">
		/// If set to <see langword="true"/>, then all string elements keys will be made lowercase.
		/// </param>
		/// <typeparam name="T">
		/// The element type (either <see cref="UnityEngine.Object"/>-derived, <see cref="System.Int32"/>, or
		/// <see cref="System.String"/>).
		/// </typeparam>
		private static bool SetHashedListBackingFieldFromHashset<T>(
			List<T> backingField, HashSet<T> value, bool ignoreCase = false
		)
		{
			if (value == null || value.Count == 0)
			{
				int oldCount = backingField.Count;
				backingField.Clear();
				return oldCount != 0;
			}
			T[] oldValue = backingField.ToArray();
			backingField.Clear();
			bool isString = typeof(T) == typeof(string);
			// TODO: correctly handle ignore case
			backingField.AddRange(
				from element in value select isString && ignoreCase ? ConvertStringKeyToLower(element) : element
			);
			if (oldValue.Length != backingField.Count)
			{
				return true;
			}
			for (int i = 0; i < oldValue.Length; ++i)
			{
				if (oldValue[i].GetHashCode() != backingField[i].GetHashCode())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Sets a backing field for a set of <see cref="System.Int32"/>s.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of <see cref="System.Int32"/> elements.</param>
		/// <param name="value">Value.</param>
		public static bool SetHashedListBackingFieldFromIntHashset(List<int> backingField, HashSet<int> value)
		{
			return SetHashedListBackingFieldFromHashset<int>(backingField, value, false);
		}

		/// <summary>
		/// Sets a backing field for a set of <see cref="UnityEngine.Object"/>s.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of <see cref="UnityEngine.Object"/> elements.</param>
		/// <param name="value">Value.</param>
		/// <typeparam name="T">A <see cref="UnityEngine.Object"/>-derived type.</typeparam>
		public static bool SetHashedListBackingFieldFromObjectHashset<T>(List<T> backingField, HashSet<T> value)
			where T : UnityEngine.Object
		{
			return SetHashedListBackingFieldFromHashset<T>(backingField, value, false);
		}

		/// <summary>
		/// Sets a backing field for a set of <see cref="System.String"/>s.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of <see cref="System.String"/> elements.</param>
		/// <param name="value">Value.</param>
		/// <param name="ignoreCase">If set to <see langword="true"/>, then all elements will be made lowercase.</param>
		public static bool SetHashedListBackingFieldFromStringHashset(
			List<string> backingField, HashSet<string> value, bool ignoreCase = false
		)
		{
			return SetHashedListBackingFieldFromHashset<string>(backingField, value, ignoreCase);
		}

		/// <summary>
		/// Sets the backing field for a public interface property that also has a proxy backing field to serialize
		/// <see cref="UnityEngine.Object"/>s that implement the interface.
		/// </summary>
		/// <returns><see langword="true"/>, if the value changed; otherwise, <see langword="false"/>.</returns>
		/// <param name="value">Value submitted to the <typeparamref name="T"/> setter.</param>
		/// <param name="interfaceBackingField">Interface backing field.</param>
		/// <param name="objectBackingField">
		/// Serialized backing field for a <see cref="UnityEngine.Object"/> that implements <typeparamref name="T"/>.
		/// </param>
		/// <param name="onAssign">Optional callback to invoke when a new value is assigned.</param>
		/// <param name="onUnassign">Optional callback to invoke when the old value is unassigned.</param>
		/// <typeparam name="T">The interface to which the backing field should be restricted.</typeparam>
		public static bool SetInterfaceBackingField<T>(
			T value,
			ref T interfaceBackingField,
			ref UnityEngine.Object objectBackingField,
			System.Action<T> onAssign = null,
			System.Action<T> onUnassign = null
		) where T : class
		{
			if (value == interfaceBackingField)
			{
				return false;
			}
			if (interfaceBackingField != null && onUnassign != null)
			{
				onUnassign(interfaceBackingField);
			}
			interfaceBackingField = value;
			objectBackingField = value as UnityEngine.Object; // NOTE: store for inspector/serialization
			if (interfaceBackingField != null && onAssign != null)
			{
				onAssign(interfaceBackingField);
			}
			return true;
		}

		/// <summary>
		/// Sets the backing field for a private <see cref="UnityEngine.Object"/> property that is a proxy for a public
		/// interface property.
		/// </summary>
		/// <param name="value">Value submitted to the <see cref="UnityEngine.Object"/> setter.</param>
		/// <param name="backingField">
		/// Serialized backing field for a <see cref="UnityEngine.Object"/> that implements <typeparamref name="T"/>.
		/// </param>
		/// <param name="setInterfaceProperty">
		/// The setter for the property taking a <typeparamref name="T"/> interface, which should call
		/// <see cref="M:Candlelight.BackingFieldUtility.SetInterfaceBackingField``1(``0,``0@,UnityEngine.Object@,System.Action{``0},System.Action{``0})" />.
		/// </param>
		/// <typeparam name="T">The interface to which the backing field should be restricted.</typeparam>
		public static void SetInterfaceBackingFieldObject<T>(
			UnityEngine.Object value, ref UnityEngine.Object backingField, System.Action<T> setInterfaceProperty
		) where T : class
		{
			if (value != null && !(value is T))
			{
				UnityEngine.Debug.LogError(
					string.Format("{0} must implement {1}.", value.GetType().FullName, typeof(T).FullName)
				);
			}
			value = (value is T) ? value : null;
			backingField = value;
			setInterfaceProperty(backingField as T);
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique keys. Use this method when
		/// you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="mutateIdentifier">A method to mutate the identifier on an object if it is not unique.</param>
		/// <param name="ignoreCase">
		/// If set to <see langword="true"/>, then all string-based keys will be made lowercase.
		/// </param>
		/// <param name="intKeyMode">Mode for handling duplicate <see cref="System.Int32"/> keys.</param>
		/// <typeparam name="T">
		/// A type with a <see cref="System.Int32"/>, <see cref="System.String"/>, or <see cref="UnityEngine.Object"/>
		/// identifier.
		/// </typeparam>
		/// <typeparam name="TId">
		/// The identifier type (either <see cref="UnityEngine.Object"/>-derived, <see cref="System.Int32"/>, or
		/// <see cref="System.String"/>).
		/// </typeparam>
		private static bool SetKeyedListBackingFieldFromArray<T, TId>(
			List<T> backingField,
			IList<T> value,
			System.Func<TId, T, T> mutateIdentifier,
			bool ignoreCase,
			IntKeyMode intKeyMode = IntKeyMode.Increment
		) where T : IIdentifiable<TId>
		{
			if (value == null || value.Count == 0)
			{
				int oldCount = backingField.Count;
				backingField.Clear();
				return oldCount != 0;
			}
			using (ListPool<T>.Scope oldValue = new ListPool<T>.Scope())
			{
				oldValue.List.AddRange(backingField);
				backingField.Clear();
				bool isString = typeof(TId) == typeof(string);
				bool isInt = !isString && typeof(TId) == typeof(int);
				if (!isInt && !isString) // NOTE: WinRT cannot use Type.IsAssignableFrom()
				{
					int nullHash = 0;
					int nullIndex = backingField.FindIndex(item => item.Identifier.GetHashCode() == nullHash);
					foreach (T newValue in value)
					{
						if (newValue == null)
						{
							if (nullIndex < 0)
							{
								backingField.Add(newValue);
								nullIndex = backingField.Count - 1;
							}
						}
						else if (
							!object.ReferenceEquals(newValue.Identifier, default(TId)) &&
							backingField.FindIndex(
								item => item.Identifier.GetHashCode() == newValue.Identifier.GetHashCode()
							) < 0
						)
						{
							backingField.Add(newValue);
							if (newValue.Identifier.GetHashCode() == nullHash)
							{
								nullIndex = backingField.Count - 1;
							}
						}
						else if (nullIndex < 0)
						{
							backingField.Add(mutateIdentifier(default(TId), newValue));
							nullIndex = backingField.Count - 1;
						}
					}
				}
				else
				{
					using (var keyedValues = new DictPool<TId, T>.Scope())
					{
						using (var stringKeys = new HashPool<string>.Scope())
						{
							TId id;
							for (int i = 0; i < value.Count; ++i)
							{
								id = value[i] == null ? default(TId) : value[i].Identifier;
								if (isString)
								{
									string key = (string)(object)id ?? string.Empty;
									if (ignoreCase && key.ContainsUppercase())
									{
										key = key.ToLower();
									}
									key = key.GetUniqueName(stringKeys.HashSet);
									stringKeys.HashSet.Add(key);
									id = (TId)(object)key;
								}
								else if (isInt)
								{
									switch (intKeyMode)
									{
									case IntKeyMode.Increment:
										while (keyedValues.Dict.ContainsKey(id))
										{
											id = (TId)(object)((int)(object)id + 1);
										}
										break;
									case IntKeyMode.SetToZero:
										if (keyedValues.Dict.ContainsKey(id))
										{
											id = (TId)(object)0;
											if (keyedValues.Dict.ContainsKey(id))
											{
												continue;
											}
										}
										break;
									}
								}
								keyedValues.Dict[id] = value[i];
							}
							foreach (KeyValuePair<TId, T> kv in keyedValues.Dict)
							{
								backingField.Add(mutateIdentifier(kv.Key, kv.Value));
							}
						}
					}
				}
				return !oldValue.List.SequenceEqual(backingField);
			}
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique <see cref="System.Int32"/>
		/// keys. Use this method when you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="mutateIdentifier">A method to mutate the identifier on an object if it is not unique.</param>
		/// <param name="mode">Mode.</param>
		/// <typeparam name="T">An integer-keyed backing field compatible object wrapper type.</typeparam>
		public static bool SetKeyedListBackingFieldFromIntKeyedArray<T>(
			List<T> backingField,
			IList<T> value,
			System.Func<int, T, T> mutateIdentifier,
			IntKeyMode mode = IntKeyMode.Increment
		) where T : IIdentifiable<int>
		{
			return SetKeyedListBackingFieldFromArray<T, int>(backingField, value, mutateIdentifier, false, mode);
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique
		/// <see cref="UnityEngine.Object"/> keys. Use this method when you need to serialize something that may be
		/// deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="mutateIdentifier">A method to mutate the identifier on an object if it is not unique.</param>
		/// <typeparam name="T">An integer-keyed backing field compatible object wrapper type.</typeparam>
		/// <typeparam name="TId">The <see cref="UnityEngine.Object"/>-derived key type.</typeparam>
		public static bool SetKeyedListBackingFieldFromObjectKeyedArray<T, TId>(
			List<T> backingField, IList<T> value, System.Func<TId, T, T> mutateIdentifier
		)
			where T : IIdentifiable<TId>
			where TId : UnityEngine.Object
		{
			return SetKeyedListBackingFieldFromArray<T, TId>(backingField, value, mutateIdentifier, false);
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique <see cref="System.String"/>
		/// keys. Use this method when you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="mutateIdentifier">A method to mutate the identifier on an object if it is not unique.</param>
		/// <param name="ignoreCase">If set to <see langword="true"/>, then all keys will be made lowercase.</param>
		/// <typeparam name="T">A string-keyed backing field compatible object wrapper type.</typeparam>
		public static bool SetKeyedListBackingFieldFromStringKeyedArray<T>(
			List<T> backingField, IList<T> value, System.Func<string, T, T> mutateIdentifier, bool ignoreCase = false
		) where T : IIdentifiable<string>
		{
			return SetKeyedListBackingFieldFromArray<T, string>(backingField, value, mutateIdentifier, ignoreCase);
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique keys. Use this method when
		/// you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable wrapper objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="factory">
		/// Method to create a new entry instance for the backing field from the key-value pair.
		/// </param>
		/// <param name="ignoreCase">
		/// If set to <see langword="true"/>, then all string-based keys will be made lowercase.
		/// </param>
		/// <typeparam name="TWrapper">An identifiable backing field compatible object wrapper type.</typeparam>
		/// <typeparam name="TId">
		/// The identifier type (either <see cref="System.Int32"/> or <see cref="System.String"/>).
		/// </typeparam>
		/// <typeparam name="TData">The data type.</typeparam>
		private static bool SetKeyedListBackingFieldFromDict<TWrapper, TId, TData>(
			List<TWrapper> backingField,
			Dictionary<TId, TData> value,
			System.Func<TId, TData, TWrapper> factory,
			bool ignoreCase = false
		)
			where TWrapper : IdentifiableBackingFieldCompatibleObjectWrapper<TId, TData>
		{
			if (value == null || value.Count == 0)
			{
				int oldCount = backingField.Count;
				backingField.Clear();
				return oldCount != 0;
			}
			TWrapper[] oldValue = backingField.ToArray();
			backingField.Clear();
			bool isString = typeof(TId) == typeof(string);
			// TODO: correctly handle ignore case
			backingField.AddRange(
				from kv in value
				select factory(isString && ignoreCase ? ConvertStringKeyToLower(kv.Key) : kv.Key, kv.Value)
			);
			if (oldValue.Length != backingField.Count)
			{
				return true;
			}
			for (int i = 0; i < oldValue.Length; ++i)
			{
				if (oldValue[i].Data.GetHashCode() != backingField[i].Data.GetHashCode())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique keys. Use this method when
		/// you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable wrapper objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="factory">
		/// Method to create a new entry instance for the backing field from the key-value pair.
		/// </param>
		/// <typeparam name="TWrapper">An integer-keyed backing field compatible object wrapper type.</typeparam>
		/// <typeparam name="TData">The data type.</typeparam>
		public static bool SetKeyedListBackingFieldFromIntKeyedDict<TWrapper, TData>(
			List<TWrapper> backingField, Dictionary<int, TData> value, System.Func<int, TData, TWrapper> factory
		) where TWrapper : IdentifiableBackingFieldCompatibleObjectWrapper<int, TData>
		{
			return SetKeyedListBackingFieldFromDict<TWrapper, int, TData>(backingField, value, factory, false);
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique keys. Use this method when
		/// you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable wrapper objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="factory">
		/// Method to create a new entry instance for the backing field from the key-value pair.
		/// </param>
		/// <typeparam name="TWrapper">
		/// A <see cref="UnityEngine.Object"/>-keyed backing field compatible object wrapper type.
		/// </typeparam>
		/// <typeparam name="TId">The <see cref="UnityEngine.Object"/>-derived key type.</typeparam>
		/// <typeparam name="TData">The data type.</typeparam>
		public static bool SetKeyedListBackingFieldFromObjectKeyedDict<TWrapper, TId, TData>(
			List<TWrapper> backingField, Dictionary<TId, TData> value, System.Func<TId, TData, TWrapper> factory
		)
			where TWrapper : IdentifiableBackingFieldCompatibleObjectWrapper<TId, TData>
			where TId : UnityEngine.Object
		{
			return SetKeyedListBackingFieldFromDict<TWrapper, TId, TData>(backingField, value, factory, false);
		}

		/// <summary>
		/// Sets a backing field for a list of identifiable objects that should have unique keys. Use this method when
		/// you need to serialize something that may be deserialized as a dictionary.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the new value differs from the old one; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="backingField">Backing field of identifiable wrapper objects.</param>
		/// <param name="value">Value.</param>
		/// <param name="factory">
		/// Method to create a new entry instance for the backing field from the key-value pair.
		/// </param>
		/// <param name="ignoreCase">If set to <see langword="true"/>, then all keys will be made lowercase.</param>
		/// <typeparam name="TWrapper">A string-keyed backing field compatible object wrapper type.</typeparam>
		/// <typeparam name="TData">The data type.</typeparam>
		public static bool SetKeyedListBackingFieldFromStringKeyedDict<TWrapper, TData>(
			List<TWrapper> backingField,
			Dictionary<string, TData> value,
			System.Func<string, TData, TWrapper> factory,
			bool ignoreCase = false
		) where TWrapper : IdentifiableBackingFieldCompatibleObjectWrapper<string, TData>
		{
			return SetKeyedListBackingFieldFromDict<TWrapper, string, TData>(backingField, value, factory, ignoreCase);
		}
	}

	/// <summary>
	/// Backing field utility class.
	/// </summary>
	/// <typeparam name="T">An <see cref="IPropertyBackingFieldCompatible"/> type.</typeparam>
	public static class BackingFieldUtility<T> where T: IPropertyBackingFieldCompatible
	{
		/// <summary>
		/// An <see cref="System.Collections.Generic.IEqualityComparer{T}"/>, provided as a convenience for evaluating
		/// equality of sequences of <typeparamref name="T"/>.
		/// </summary>
		public class CollectionComparer : IEqualityComparer<T>
		{
			/// <summary>
			/// Determines if the two specified <typeparamref name="T"/> are equivalent in terms of their serialized
			/// properties.
			/// </summary>
			/// <returns>
			/// <see langword="true"/> if the two <typeparamref name="T"/> are equal; otherwise,
			/// <see langword="false"/>.
			/// </returns>
			/// <param name="a">The first <typeparamref name="T"/>.</param>
			/// <param name="b">The second <typeparamref name="T"/>.</param>
			public bool Equals(T a, T b)
			{
				return a.GetSerializedPropertiesHash() == b.GetSerializedPropertiesHash();
			}
			/// <summary>
			/// Gets the hash code of the specified <typeparamref name="T"/> in terms of its serialized properties.
			/// </summary>
			/// <returns>The hash code.</returns>
			/// <param name="obj">Object.</param>
			public int GetHashCode(T obj)
			{
				return obj == null ? 0 : obj.GetSerializedPropertiesHash();
			}
		}

		#region Backing Fields
		private static CollectionComparer s_Comparer = null;
		#endregion
		/// <summary>
		/// Gets the comparer for this class's type.
		/// </summary>
		/// <value>The comparer for this class's type.</value>
		public static CollectionComparer Comparer
		{
			get
			{
				if (s_Comparer == null)
				{
					s_Comparer = new CollectionComparer();
				}
				return s_Comparer;
			}
		}
	}
}