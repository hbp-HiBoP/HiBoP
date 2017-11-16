// 
// ObjectX.cs
// 
// Copyright (c) 2012-2017, Candlelight Interactive, LLC
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

namespace Candlelight
{
	/// <summary>
	/// An extension class for <see cref="System.Object"/>s and <see cref="UnityEngine.Object"/>s.
	/// </summary>
	public static class ObjectX
	{
#if UNITY_EDITOR
		/// <summary>
		/// Initializes the <see cref="ObjectX"/> class.
		/// </summary>
		static ObjectX()
		{
			System.Text.RegularExpressions.Regex matchCandlelightClass =
				new System.Text.RegularExpressions.Regex(@"^Candlelight\b");
			using (var types = new HashPool<System.Type>.Scope())
			{
				GetAllTypes(types.HashSet);
				foreach (var type in types.HashSet)
				{
					if (
						!type.IsSubclassOf(typeof(UnityEngine.ScriptableObject)) ||
						!matchCandlelightClass.IsMatch(type.Namespace ?? "")
					)
					{
						continue;
					}
					System.Reflection.ConstructorInfo constructor = type.GetConstructor(
						System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
						null,
						System.Type.EmptyTypes,
						null
					);
					if (constructor == null || constructor.IsPublic)
					{
						UnityEngine.Debug.LogError(
							string.Format("<b>{0}</b> has no protected, parameterless constructor.", type)
						);
					}
				}
			}
		}

		/// <summary>
		/// A regular expression to match editor assembly names.
		/// </summary>
		private static readonly System.Text.RegularExpressions.Regex s_MatchEditorAssemblyName =
			new System.Text.RegularExpressions.Regex(@"(^UnityEditor\.)|(-Editor\b)");
#endif
		/// <summary>
		/// A regular expression to match an instance's prefab name
		/// </summary>
		private static readonly System.Text.RegularExpressions.Regex s_MatchPrefabName =
			new System.Text.RegularExpressions.Regex(@".+(?=\s*\(Clone\))|.+");

		/// <summary>
		/// Determine if the two specified sequences are equal.
		/// </summary>
		/// <remarks>
		/// Use this method when comparing list fields on two instances inside of IEquatable&lt;T&gt;.Equals().
		/// </remarks>
		/// <returns>
		/// <see langword="true"/> if the two lists contain equal elements or are both <see langword="null"/>;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="list1">List1.</param>
		/// <param name="list2">List2.</param>
		/// <typeparam name="T">The element type.</typeparam>
		public static bool AreObjectSequencesEqual<T>(IList<T> list1, IList<T> list2) where T : UnityEngine.Object
		{
			if (list1 == null)
			{
				return list2 == null;
			}
			else if (list2 == null)
			{
				return false;
			}
			int count = list1.Count;
			if (count != list2.Count)
			{
				return false;
			}
			for (int i = 0; i < count; ++i)
			{
				if (!ReferenceEquals(list1[i], list2[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Determine if the two specified sequences are equal.
		/// </summary>
		/// <remarks>
		/// Use this method when comparing list fields on two instances inside of IEquatable&lt;T&gt;.Equals().
		/// </remarks>
		/// <returns>
		/// <see langword="true"/> if the two lists contain equal elements or are both <see langword="null"/>;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="list1">List1.</param>
		/// <param name="list2">List2.</param>
		/// <typeparam name="T">The element type.</typeparam>
		public static bool AreStructSequencesEqual<T>(IList<T> list1, IList<T> list2) where T : struct, System.IEquatable<T>
		{
			if (list1 == null)
			{
				return list2 == null;
			}
			else if (list2 == null)
			{
				return false;
			}
			int count = list1.Count;
			if (count != list2.Count)
			{
				return false;
			}
			for (int i = 0; i < count; ++i)
			{
				if (!list1[i].Equals(list2[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		public static int GenerateHashCode(int hash1, int hash2)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		/// <param name="hash3">Hash3.</param>
		public static int GenerateHashCode(int hash1, int hash2, int hash3)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			list.Add(hash3);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		/// <param name="hash3">Hash3.</param>
		/// <param name="hash4">Hash4.</param>
		public static int GenerateHashCode(int hash1, int hash2, int hash3, int hash4)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			list.Add(hash3);
			list.Add(hash4);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		/// <param name="hash3">Hash3.</param>
		/// <param name="hash4">Hash4.</param>
		/// <param name="hash5">Hash5.</param>
		public static int GenerateHashCode(int hash1, int hash2, int hash3, int hash4, int hash5)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			list.Add(hash3);
			list.Add(hash4);
			list.Add(hash5);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		/// <param name="hash3">Hash3.</param>
		/// <param name="hash4">Hash4.</param>
		/// <param name="hash5">Hash5.</param>
		/// <param name="hash6">Hash6.</param>
		public static int GenerateHashCode(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			list.Add(hash3);
			list.Add(hash4);
			list.Add(hash5);
			list.Add(hash6);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		/// <param name="hash3">Hash3.</param>
		/// <param name="hash4">Hash4.</param>
		/// <param name="hash5">Hash5.</param>
		/// <param name="hash6">Hash6.</param>
		/// <param name="hash7">Hash7.</param>
		public static int GenerateHashCode(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6, int hash7)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			list.Add(hash3);
			list.Add(hash4);
			list.Add(hash5);
			list.Add(hash6);
			list.Add(hash7);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="hash1">Hash1.</param>
		/// <param name="hash2">Hash2.</param>
		/// <param name="hash3">Hash3.</param>
		/// <param name="hash4">Hash4.</param>
		/// <param name="hash5">Hash5.</param>
		/// <param name="hash6">Hash6.</param>
		/// <param name="hash7">Hash7.</param>
		/// <param name="hash8">Hash8.</param>
		public static int GenerateHashCode(
			int hash1, int hash2, int hash3, int hash4, int hash5, int hash6, int hash7, int hash8
		)
		{
			List<int> list = ListPool<int>.Get();
			list.Add(hash1);
			list.Add(hash2);
			list.Add(hash3);
			list.Add(hash4);
			list.Add(hash5);
			list.Add(hash6);
			list.Add(hash7);
			list.Add(hash8);
			int result = GenerateHashCode(list);
			ListPool<int>.Release(list);
			return result;
		}

		/// <summary>
		/// Generates a hash code from the hash codes for an object's fields.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="fieldHashes">Hash codes for the fields on an object being hashed.</param>
		public static int GenerateHashCode(IList<int> fieldHashes)
		{
			int result = 17;
			for (int i = 0; i < fieldHashes.Count; ++i)
			{
				result = result * 23 + fieldHashes[i];
			}
			return result;
		}

		/// <summary>
		/// Generates a hash code for a list or array field.
		/// </summary>
		/// <returns>A hash code.</returns>
		/// <param name="listField">List field.</param>
		/// <typeparam name="T">The element type.</typeparam>
		public static int GenerateHashCode<T>(IList<T> listField)
		{
			int typeCode = typeof(T).GetHashCode();
			if (listField == null)
			{
				return GenerateHashCode(typeCode, -1);
			}
			int result = GenerateHashCode(typeCode, listField.Count.GetHashCode());
			for (int i = 0; i < listField.Count; ++i)
			{
				result = GenerateHashCode(result, listField[i] == null ? typeCode : listField[i].GetHashCode());
			}
			return result;
		}

		/// <summary>
		/// Gets all run-time types in the current application.
		/// </summary>
		/// <remarks>This method does nothing at run-time.</remarks>
		/// <param name="allTypes">All run-time types in the current application.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void GetAllTypes(System.Collections.Generic.HashSet<System.Type> allTypes)
		{
			allTypes.Clear();
#if UNITY_EDITOR
			foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				if (s_MatchEditorAssemblyName.IsMatch(assembly.FullName))
				{
					continue;
				}
				foreach (System.Type t in assembly.GetTypes())
				{
					allTypes.Add(t);
				}
			}
#endif
		}

		/// <summary>
		/// Gets the next suitable inactive object in the pool. Instantiates a new one if none is available.
		/// </summary>
		/// <returns>The next suitable inactive object from the pool.</returns>
		/// <param name="pool">Pool.</param>
		/// <param name="prefab">
		/// Prefab to instantiate if no instance is available in pool. If <see langword="null"/>, then an empty GameObject with the
		/// component will be created and used.
		/// </param>
		/// <param name="parent">
		/// Optional object that should become the parent of the retrieved instance. Set this value in particular for
		/// e.g., UI prefabs.
		/// </param>
		/// <param name="isElementSuitable">
		/// Optional predicate to determine if an inactive item in the pool is suitable for use.
		/// </param>
		/// <typeparam name="T">The type of object in the pool.</typeparam>
		public static T GetFromPool<T>(
			List<T> pool, T prefab, UnityEngine.Transform parent = null, System.Predicate<T> isElementSuitable = null
		) where T : UnityEngine.Component
		{
			T result;
			if (isElementSuitable != null)
			{
				result = pool.Find(item => item.gameObject.activeSelf == false && isElementSuitable(item));
			}
			else
			{
				result = pool.Find(item => item != null && item.gameObject.activeSelf == false);
			}
			if (result == null)
			{
				if (prefab == null)
				{
					result = new UnityEngine.GameObject(
						string.Format("<Pooled {0} {1}>", typeof(T).FullName, pool.Count), typeof(T)
					).GetComponent<T>();
				}
				else
				{
					result = UnityEngine.Object.Instantiate(prefab) as T;
				}
				result.transform.SetParent(parent, false);
				result.transform.SetAsLastSibling();
				pool.Add(result);
			}
			result.gameObject.SetActive(true);
			return result;
		}

		/// <summary>
		/// Gets the name of the prefab associated with the supplied instance.
		/// </summary>
		/// <returns>The prefab name.</returns>
		/// <param name="instance">
		/// An <see cref="UnityEngine.Object"/> instance with a name in the form "Object Name (Clone)".
		/// </param>
		public static string GetPrefabName(this UnityEngine.Object instance)
		{
			return s_MatchPrefabName.Match(instance.name).Value;
		}

		/// <summary>
		/// Opens a reference web page generated for the specified object.
		/// </summary>
		/// <remarks>This method assumes the page in question was generated via SHFB.</remarks>
		/// <param name="obj">A <see cref="UnityEngine.Object"/>.</param>
		/// <param name="product">Product name (first parameter in format string).</param>
		/// <param name="urlFormat">
		/// A <see cref="System.String"/> specifying the URL format. Index 0 is for the product name and index 1 is for
		/// the type name.
		/// </param>
		public static void OpenReferencePage(
			this UnityEngine.Object obj,
			string product,
			string urlFormat = "http://candlelightinteractive.com/docs/{0}/html/T_{1}.htm?ref=editor"
		)
		{
			UnityEngine.Application.OpenURL(
				string.Format(urlFormat, product, obj.GetType().FullName.Replace('.', '_'))
			);
		}

		#region Obsolete
		[System.Obsolete("Manually implement your own equality comparison.", true)]
		public static bool Equals<T>(ref T thisObj, object otherObj) where T : struct
		{
			return false;
		}
		#endregion
	}
}