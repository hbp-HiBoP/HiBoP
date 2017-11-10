// 
// ReflectionX.cs
// 
// Copyright (c) 2015-2016, Candlelight Interactive, LLC
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
// This file contains a class with extension methods for reflection.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Candlelight
{
	/// <summary>
	/// Extension methods for common reflection tasks.
	/// </summary>
	public static class ReflectionX
	{
		/// <summary>
		/// The binding flags for instance fields and properties.
		/// </summary>
		public const BindingFlags instanceBindingFlags =
			BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		/// <summary>
		/// The binding flags for static fields and properties.
		/// </summary>
		public const BindingFlags staticBindingFlags =
			BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		#region Backing Fields
		private static ReadOnlyCollection<System.Type> s_AllTypes = null;
		private static ReadOnlyCollection<Assembly> s_UnityRuntimeAssemblies = null;
		#endregion

		/// <summary>
		/// Gets all types in the current application.
		/// </summary>
		/// <value>All types in the current application.</value>
		public static ReadOnlyCollection<System.Type> AllTypes
		{
			get
			{
				if (s_AllTypes == null)
				{
					HashSet<System.Type> allTypes = new HashSet<System.Type>();
					foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
					{
						foreach (System.Type t in assembly.GetTypes())
						{
							allTypes.Add(t);
						}
					}
					s_AllTypes = new ReadOnlyCollection<System.Type>(allTypes.ToArray());
				}
				return s_AllTypes;
			}
		}
		/// <summary>
		/// Gets the Unity runtime assemblies.
		/// </summary>
		/// <value>The Unity runtime assemblies.</value>
		public static ReadOnlyCollection<Assembly> UnityRuntimeAssemblies
		{
			get
			{
				if (s_UnityRuntimeAssemblies == null)
				{
					s_UnityRuntimeAssemblies = new ReadOnlyCollection<Assembly>(
						(
							from assembly in System.AppDomain.CurrentDomain.GetAssemblies() select assembly
						).Where(
							assembly => assembly.GetName().Name.StartsWith("UnityEngine")
						).ToArray()
					);
				}
				return s_UnityRuntimeAssemblies;
			}
		}

		/// <summary>
		/// Gets the custom attributes.
		/// </summary>
		/// <remarks>
		/// http://msdn.microsoft.com/en-us/library/dwc6ew1d(v=vs.90).aspx
		/// </remarks>
		/// <returns>The number of custom attributes of the specified type found.</returns>
		/// <param name="provider">Provider.</param>
		/// <param name="attributes">A list of <typeparamref name="T"> to populate.</param>
		/// <param name="inherit">
		/// <see langword="true"/> if inherited attributes should be included; otherwise, <see langword="false"/>.
		/// </param>
		/// <typeparam name='T'>The type of the custom attributes desired.</typeparam>
		public static int GetCustomAttributes<T>(
			this ICustomAttributeProvider provider, List<T> attributes, bool inherit = false
		) where T : System.Attribute
		{
			attributes.Clear();
			if (provider == null)
			{
				return 0;
			}
			attributes.AddRange(provider.GetCustomAttributes(typeof(T), inherit).Cast<T>());
//			if (attributes == null)
//			{   // WORKAROUND: Due to a bug in the code for retrieving attributes from a dynamic generated parameter,
//				// GetCustomAttributes can return an instance of object[] instead of T[], and hence the cast above will
//				// return null.
//				return new T[0];
//			}
//			return attributes;
			return attributes.Count;
		}

		/// <summary>
		/// Gets the field value on an instance.
		/// </summary>
		/// <returns>The field value on an instance.</returns>
		/// <param name="provider">Provider.</param>
		/// <param name="fieldName">Field name.</param>
		/// <param name="bindingAttr">Binding flags.</param>
		/// <typeparam name="T">The type of the field.</typeparam>
		public static T GetFieldValue<T>(
			this object provider, string fieldName, BindingFlags bindingAttr = instanceBindingFlags
		)
		{
			return GetFieldValue<T>(provider.GetType(), provider, fieldName, bindingAttr);
		}
		
		/// <summary>
		/// Gets the field value on a provider
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="provider">Provider.</param>
		/// <param name="fieldName">Field name.</param>
		/// <param name="bindingAttr">Binding attr.</param>
		/// <typeparam name="T">The type of the field.</typeparam>
		/// <exception cref="System.ArgumentNullException">
		/// Is thrown if the type or fieldName is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentException">Is thrown if fieldName is empty.</exception>
		private static T GetFieldValue<T>(
			System.Type type, object provider, string fieldName, BindingFlags bindingAttr
		)
		{
			if (type == null)
			{
				throw new System.ArgumentNullException("type");
			}
			else if (fieldName == null)
			{
				throw new System.ArgumentNullException("fieldName");
			}
			else if (fieldName.Length == 0)
			{
				throw new System.ArgumentException("No field name specified.", "fieldName");
			}
			return (T)type.GetField(fieldName, bindingAttr).GetValue(provider);
		}

		/// <summary>
		/// Gets the instance field defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The instance field.</returns>
		/// <param name="type">Type.</param>
		/// <param name="fieldName">Field name.</param>
		public static FieldInfo GetInstanceField(this System.Type type, string fieldName)
		{
			if (type == null)
			{
				return null;
			}
			FieldInfo result = type.GetField(fieldName, instanceBindingFlags);
			while (type.BaseType != null && result == null)
			{
				result = type.GetField(fieldName, instanceBindingFlags);
				type = type.BaseType;
			}
			return result;
		}

		/// <summary>
		/// Gets the instance method defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The instance method.</returns>
		/// <param name="type">Type.</param>
		/// <param name="methodName">Method name.</param>
		public static MethodInfo GetInstanceMethod(this System.Type type, string methodName)
		{
			if (type == null)
			{
				return null;
			}
			MethodInfo result = type.GetMethod(methodName, instanceBindingFlags);
			while (type.BaseType != null && result == null)
			{
				result = type.GetMethod(methodName, instanceBindingFlags);
				type = type.BaseType;
			}
			return result;
		}

		/// <summary>
		/// Gets the instance method defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The instance method.</returns>
		/// <param name="type">Type.</param>
		/// <param name="methodName">Method name.</param>
		/// <param name="types">Parameter types.</param>
		public static MethodInfo GetInstanceMethod(this System.Type type, string methodName, System.Type[] types)
		{
			if (type == null)
			{
				return null;
			}
			MethodInfo result = type.GetMethod(methodName, instanceBindingFlags, null, types, null);
			while (type.BaseType != null && result == null)
			{
				result = type.GetMethod(methodName, instanceBindingFlags, null, types, null);
				type = type.BaseType;
			}
			return result;
		}

		/// <summary>
		/// Gets the instance property defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The instance property.</returns>
		/// <param name="type">Type.</param>
		/// <param name="propertyName">Property name.</param>
		public static PropertyInfo GetInstanceProperty(this System.Type type, string propertyName)
		{
			if (type == null)
			{
				return null;
			}
			PropertyInfo result = type.GetProperty(propertyName, instanceBindingFlags);
			while (type.BaseType != null && result == null)
			{
				result = type.GetProperty(propertyName, instanceBindingFlags);
				type = type.BaseType;
			}
			return result;
		}

		/// <summary>
		/// Gets the property value on an instance.
		/// </summary>
		/// <returns>The property value on an instance.</returns>
		/// <param name="provider">Provider.</param>
		/// <param name="propertyName">Property name.</param>
		/// <param name="bindingAttr">Binding flags.</param>
		/// <param name="index">Index.</param>
		/// <typeparam name="T">The type of the property.</typeparam>
		public static T GetPropertyValue<T>(
			this object provider,
			string propertyName,
			BindingFlags bindingAttr = instanceBindingFlags,
			object[] index = null
		)
		{
			return GetPropertyValue<T>(provider.GetType(), provider, propertyName, bindingAttr, index);
		}
		
		/// <summary>
		/// Gets the property value on a provider.
		/// </summary>
		/// <returns>The property value on an instance.</returns>
		/// <param name="provider">Provider.</param>
		/// <param name="propertyName">Property name.</param>
		/// <param name="bindingAttr">Binding flags.</param>
		/// <param name="index">Index.</param>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <exception cref="System.ArgumentNullException">
		/// Is thrown if the provider or propertyName is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentException">Is thrown if propertyName is empty.</exception>
		private static T GetPropertyValue<T>(
			System.Type type, object provider, string propertyName, BindingFlags bindingAttr, object[] index
		)
		{
			if (provider == null)
			{
				throw new System.ArgumentNullException("provider");
			}
			else if (propertyName == null)
			{
				throw new System.ArgumentNullException("propertyName");
			}
			else if (propertyName.Length == 0)
			{
				throw new System.ArgumentException("No property name specified.", "propertyName");
			}
			return (T)provider.GetType().GetProperty(propertyName, bindingAttr).GetValue(provider, index);
		}

		/// <summary>
		/// Gets the static field defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The static field.</returns>
		/// <param name="type">Type.</param>
		/// <param name="fieldName">Field name.</param>
		public static FieldInfo GetStaticField(this System.Type type, string methodName)
		{
			return type == null ? null : type.GetField(methodName, staticBindingFlags);
		}
		
		/// <summary>
		/// Gets the field value on a class.
		/// </summary>
		/// <returns>The field value on a class.</returns>
		/// <param name="type">Type.</param>
		/// <param name="fieldName">Field name.</param>
		/// <param name="bindingAttr">Binding flags.</param>
		/// <typeparam name="T">The type of the field.</typeparam>
		public static T GetStaticFieldValue<T>(
			this System.Type type, string fieldName, BindingFlags bindingAttr = staticBindingFlags
		)
		{
			return GetFieldValue<T>(type, null, fieldName, bindingAttr);
		}

		/// <summary>
		/// Gets the static method defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The static method.</returns>
		/// <param name="type">Type.</param>
		/// <param name="methodName">Method name.</param>
		public static MethodInfo GetStaticMethod(this System.Type type, string methodName)
		{
			return type == null ? null : type.GetMethod(methodName, staticBindingFlags);
		}

		/// <summary>
		/// Gets the static method defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The static method.</returns>
		/// <param name="type">Type.</param>
		/// <param name="methodName">Method name.</param>
		/// <param name="types">Parameter types.</param>
		public static MethodInfo GetStaticMethod(this System.Type type, string methodName, System.Type[] types)
		{
			if (type == null)
			{
				return null;
			}
			MethodInfo result = type.GetMethod(methodName, staticBindingFlags, null, types, null);
			while (type.BaseType != null && result == null)
			{
				result = type.GetMethod(methodName, staticBindingFlags, null, types, null);
				type = type.BaseType;
			}
			return result;
		}

		/// <summary>
		/// Gets the static property defined on the specified <paramref name="type"/> or a base class.
		/// </summary>
		/// <returns>The static property.</returns>
		/// <param name="type">Type.</param>
		/// <param name="propertyName">Property name.</param>
		public static PropertyInfo GetStaticProperty(this System.Type type, string propertyName)
		{
			return type == null ? null : type.GetProperty(propertyName, staticBindingFlags);
		}

		/// <summary>
		/// Gets the property value on a class.
		/// </summary>
		/// <returns>The property value on a class.</returns>
		/// <param name="provider">Provider.</param>
		/// <param name="propertyName">Property name.</param>
		/// <param name="bindingAttr">Binding flags.</param>
		/// <param name="index">Index.</param>
		/// <typeparam name="T">The type of the property.</typeparam>
		public static T GetStaticPropertyValue<T>(
			this System.Type type,
			string propertyName,
			BindingFlags bindingAttr = instanceBindingFlags,
			object[] index = null
		)
		{
			return GetPropertyValue<T>(type, null, propertyName, bindingAttr, index);
		}
	}
}