// 
// PropertyBackingFieldAttribute.cs
// 
// Copyright (c) 2014-2015, Candlelight Interactive, LLC
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
using System.Linq;
using System.Reflection;

namespace Candlelight
{
	/// <summary>
	/// A <see cref="UnityEngine.PropertyAttribute"/> to specify that a serialized field is a backing field for a
	/// property. A serialized backing field decorated with this attribute will trigger its getter and setter when the
	/// user makes changes from the inspector.
	/// </summary>
	/// <remarks>
	/// In order to use this attribute, the property to which the field corresponds must implement both get and
	/// set methods (any access modifiers are okay). These methods can be implemented either as a property or as
	/// methods. For example, either of the following examples is valid:
	/// <code>
	/// public class MyComponent : UnityEngine.MonoBehaviour
	/// {
	///		[UnityEngine.SerializeField, Candlelight.PropertyBackingField]
	///		private int m_MyInt = 0;
	///		
	///		public int MyInt
	///		{
	///			get { return m_MyInt; }
	///			set { m_MyInt = Mathf.Max(0, value); }
	///		}
	/// }
	/// </code>
	/// <code>
	/// public class MyComponent : UnityEngine.MonoBehaviour
	/// {
	///		[UnityEngine.SerializeField, Candlelight.PropertyBackingField]
	///		private int m_MyInt = 0;
	///		
	///		public int GetMyInt()
	///		{
	///			return m_MyInt;
	///		}
	///		
	///		public void SetMyInt()
	///		{
	///			m_MyInt = Mathf.Max(0, value);
	///		}
	/// }
	/// </code>
	/// The corresponding custom property drawer ensures to the best of its ability that the object is properly dirtied
	/// so it is compatible with undo/redo and prefab overrides.
	/// </remarks>
	public class PropertyBackingFieldAttribute : PropertyAttribute
	{
		/// <summary>
		/// Gets an optional <see cref="UnityEngine.PropertyAttribute"/> that specifies what
		/// <see cref="UnityEditor.PropertyDrawer"/> should be used for the decorated field.
		/// </summary>
		/// <value>An optional <see cref="UnityEngine.PropertyAttribute"/> that specifies what
		/// <see cref="UnityEditor.PropertyDrawer"/> should be used for the decorated field.</value>
		public PropertyAttribute OverrideAttribute { get; private set; }
		/// <summary>
		/// Gets the name of the property for which the decorated field is a backing field.
		/// </summary>
		/// <value>The name of the property for which the decorated field is a backing field.</value>
		public string PropertyName { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyBackingFieldAttribute"/> class that
		/// uses the default <see cref="UnityEditor.PropertyDrawer"/> for the field type. This constructor assumes that
		/// the backing field name starts with "m_" or "_" and that the property name otherwise matches. For example, a
		/// field m_Character or _Character could refer to either a property Character { get; set; } or a pair of
		/// methods GetCharacter() and SetCharacter().
		/// </summary>
		public PropertyBackingFieldAttribute()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyBackingFieldAttribute"/> class that
		/// uses the default <see cref="UnityEditor.PropertyDrawer"/> for the field type.
		/// </summary>
		/// <param name="propertyName">
		/// Name of the getter/setter property corresponding to the backing field, or name of getter/setter methods. For
		/// example, "Character" could refer to either a property Character { get; set; } or a pair of methods
		/// GetCharacter() and SetCharacter().
		/// </param>
		public PropertyBackingFieldAttribute(string propertyName)
		{
			this.PropertyName = propertyName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyBackingFieldAttribute"/> class that should use a custom
		/// <see cref="UnityEditor.PropertyDrawer"/> associated with another <see cref="UnityEngine.PropertyAttribute"/> 
		/// to display the decorated field in the inspector. This constructor assumes that the backing field name starts 
		/// with "m_" or "_" and that the property name otherwise matches. For example, a field m_Character or
		/// _Character could refer to either a property Character { get; set; } or a pair of methods GetCharacter() and
		///  SetCharacter().
		/// </summary>
		/// <param name="propertyAttributeType">
		/// A <see cref="UnityEngine.PropertyAttribute"/> <see cref="System.Type"/> to specify what custom
		/// <see cref="UnityEditor.PropertyDrawer"/> should be used in the inspector.
		/// </param>
		/// <param name="constructorArguments">Parameters for the override attribute's constructor.</param>
		public PropertyBackingFieldAttribute(
			System.Type propertyAttributeType, params object[] constructorArguments
		)
		{
#if UNITY_EDITOR
			// try to find the matching constructor
			ConstructorInfo constructor = null;
			System.Type[] suppliedArgTypes =
				(from param in constructorArguments select param == null ? typeof(object) : param.GetType()).ToArray();
			foreach (ConstructorInfo ctor in propertyAttributeType.GetConstructors())
			{
				ParameterInfo[] ctorParams = ctor.GetParameters();
				System.Type paramArrayType = (
					ctorParams.LastOrDefault() != null &&
					ctorParams.Last().GetCustomAttributes(typeof(System.ParamArrayAttribute), false).Length > 0
				) ? ctorParams.Last().ParameterType : null;
				constructor = ctor;
				// first check explicitly supplied arguments
				for (int i=0; i<suppliedArgTypes.Length; ++i)
				{
					// if there are more supplied arguments than parameter types, see if they match param type
					if (
						i >= ctorParams.Length &&
						(paramArrayType == null || !paramArrayType.IsAssignableFrom(suppliedArgTypes[i]))
					)
					{
						constructor = null;
						continue;
					}
					// if supplied argument type is mismatch, then constructor is a bad match
					if (
						!ctorParams[i].ParameterType.IsAssignableFrom(suppliedArgTypes[i]) ||
						(suppliedArgTypes[i] == null && !ctorParams[i].ParameterType.IsClass)
					)
					{
						constructor = null;
					}
				}
				// if candidate is a match, see if it has any further parameters
				if (constructor != null && ctorParams.Length > suppliedArgTypes.Length)
				{
					List<object> newArgs = new List<object>(constructorArguments);
					for (int i=suppliedArgTypes.Length; i<ctorParams.Length; ++i)
					{
						// candidate is a mismatch if the argument is not optional, or is not the params argument
						if (!ctorParams[i].IsOptional && !(paramArrayType != null && i == ctorParams.Length - 1))
						{
							constructor = null;
						}
						else if (ctorParams[i].IsOptional)
						{
							newArgs.Add(ctorParams[i].DefaultValue);
						}
					}
					// if candidate is still a match, append missing arguments
					if (constructor != null)
					{
						constructorArguments = newArgs.ToArray();
					}
				}
				// break out if the candidate is still valid
				if (constructor != null)
				{
					break;
				}
			}
			if (constructor == null)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				foreach (System.Type type in suppliedArgTypes)
				{
					sb.AppendFormat(", {0}", type);
				}
				throw new System.ArgumentException(
					"constructorArguments",
					string.Format(
						"No constructor found for {0} matching method signature [{1}].",
						propertyAttributeType, sb.Length > 0 ? sb.ToString().Substring(2) : ""
					)
				);
			}
			this.OverrideAttribute = constructor.Invoke(constructorArguments) as PropertyAttribute;
#endif
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyBackingFieldAttribute"/> class that should use a custom
		/// <see cref="UnityEditor.PropertyDrawer"/> associated with another <see cref="UnityEngine.PropertyAttribute"/> 
		/// to display the decorated field in the inspector.
		/// </summary>
		/// <param name="propertyName">
		/// Name of the getter/setter property corresponding to the backing field, or name of getter/setter methods. For
		/// example, "Character" could refer to either a property Character { get; set; } or a pair of methods
		/// GetCharacter() and SetCharacter().
		/// </param>
		/// <param name="propertyAttributeType">
		/// A <see cref="UnityEngine.PropertyAttribute"/> <see cref="System.Type"/> to specify what custom
		/// <see cref="UnityEditor.PropertyDrawer"/> should be used in the inspector.
		/// </param>
		/// <param name="constructorArguments">Parameters for the override attribute's constructor.</param>
		public PropertyBackingFieldAttribute(
			string propertyName, System.Type propertyAttributeType, params object[] constructorArguments
		) : this(propertyAttributeType, constructorArguments)
		{
			this.PropertyName = propertyName;
		}
	}
}