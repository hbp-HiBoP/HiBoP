// 
// SerializedPropertyX.cs
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
// 
// This file contains a class with extension methods for SerializedProperty.

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Candlelight
{
	/// <summary>
	/// Extension methods for SerializedProperty.
	/// </summary>
	public static class SerializedPropertyX
	{
		/// <summary>
		/// The CustomPropertyDrawerAttribute type field.
		/// </summary>
		private static readonly FieldInfo s_CustomPropertyDrawerTypeField =
			typeof(CustomPropertyDrawer).GetInstanceField("m_Type");
		/// <summary>
		/// The CustomPropertyDrawerAttribute use for children field.
		/// </summary>
		private static readonly FieldInfo s_CustomPropertyDrawerUseForChildrenField =
			typeof(CustomPropertyDrawer).GetInstanceField("m_UseForChildren");
		/// <summary>
		/// The DecoratorDrawer attribute field.
		/// </summary>
		private static readonly FieldInfo s_DecoratorDrawerAttributeField =
			typeof(DecoratorDrawer).GetInstanceField("m_Attribute");
		/// <summary>
		/// A dictionary mapping property attribute types to DecoratorDrawer types.
		/// </summary>
		private static readonly Dictionary<System.Type, System.Type> s_DecoratorsForEachType =
			new Dictionary<System.Type, System.Type>();
		/// <summary>
		/// A dictionary mapping types to their corresponding drawers; keys may be PropertyAttributes or field types.
		/// </summary>
		private static readonly Dictionary<System.Type, System.Type> s_DrawersForEachType =
			new Dictionary<System.Type, System.Type>();
		/// <summary>
		/// The fields to copy for each type when getting a value from a serialized property.
		/// </summary>
		private static Dictionary<System.Type, ReadOnlyCollection<FieldInfo>> s_FieldsToCopyForEachType =
			new Dictionary<System.Type, ReadOnlyCollection<FieldInfo>>();
		/// <summary>
		/// The last serializable field for each known type.
		/// </summary>
		private static readonly Dictionary<System.Type, FieldInfo> s_LastSerializableFields =
			new Dictionary<System.Type, FieldInfo>();
		/// <summary>
		/// A regular expression to match an array element in the middle of a serialized property path.
		/// </summary>
		private static readonly Regex s_MatchArrayElement = new Regex(@"\.Array\.data\[(?<index>\d+)\]");
		/// <summary>
		/// A regular expression to match an array element at the end of a path.
		/// </summary>
		private static readonly Regex s_MatchTerminalArrayElement = new Regex(@"\[(?<index>\d+)\]$");
		/// <summary>
		/// A regular expression to match an array size property at the end of a path.
		/// </summary>
		private static readonly Regex s_MatchTerminalArraySize = new Regex(@"\.Array\.size$");
		/// <summary>
		/// A regular expression to match the terminal property in nested serialized property path.
		/// </summary>
		private static readonly Regex s_MatchTerminalProperty =
			new Regex(
				@"(?<prefix>\.)?(?<name>\w+)\.Array\.data\[\d+\]$" + "|" +
				@"(?<prefix>\.)?(?<name>\w+)\.Array\.size$" + "|" +
				@"(?<prefix>\.)?(?<name>\w+)$"
			);
		/// <summary>
		/// The PropertyDrawer attribute field.
		/// </summary>
		private static readonly FieldInfo s_PropertyDrawerAttributeField =
			typeof(PropertyDrawer).GetInstanceField("m_Attribute");
		/// <summary>
		/// The PropertyDrawer field info field.
		/// </summary>
		private static readonly FieldInfo s_PropertyDrawerFieldInfoField =
			typeof(PropertyDrawer).GetInstanceField("m_FieldInfo");
		/// <summary>
		/// Types that are natively serializable and a corresponding SerializedPropertyType.
		/// </summary>
		private static readonly Dictionary<System.Type, SerializedPropertyType> s_SerializableTypes =
			new Dictionary<System.Type, SerializedPropertyType>()
		{
			{ typeof(AnimationCurve), SerializedPropertyType.AnimationCurve },
			{ typeof(bool), SerializedPropertyType.Boolean },
			{ typeof(Bounds), SerializedPropertyType.Bounds },
			{ typeof(char), SerializedPropertyType.Character },
			{ typeof(Color), SerializedPropertyType.Color },
			{ typeof(float), SerializedPropertyType.Float },
			{ typeof(int), SerializedPropertyType.Integer },
			{ typeof(LayerMask), SerializedPropertyType.LayerMask },
			{ typeof(Object), SerializedPropertyType.ObjectReference },
			{ typeof(Quaternion), SerializedPropertyType.Quaternion },
			{ typeof(Rect), SerializedPropertyType.Rect },
			{ typeof(string), SerializedPropertyType.String },
			{ typeof(Vector2), SerializedPropertyType.Vector2 },
			{ typeof(Vector3), SerializedPropertyType.Vector3 },
			{ typeof(Vector4), SerializedPropertyType.Vector4 }
		};
		/// <summary>
		/// The value getters for different serialized property types.
		/// </summary>
		private static readonly Dictionary<SerializedPropertyType, System.Func<SerializedProperty, object>> s_ValueGetters =
			new Dictionary<SerializedPropertyType, System.Func<SerializedProperty, object>>()
		{
			{ SerializedPropertyType.AnimationCurve, prop => (object)prop.animationCurveValue },
			{ SerializedPropertyType.ArraySize, prop => (object)prop.intValue },
			{ SerializedPropertyType.Boolean, prop => (object)prop.boolValue },
			{ SerializedPropertyType.Bounds, prop => (object)prop.boundsValue },
			{ SerializedPropertyType.Character, prop => (object)prop.intValue },
			{ SerializedPropertyType.Color, prop => (object)prop.colorValue },
			{ SerializedPropertyType.Enum, prop => (object)prop.intValue },
			{ SerializedPropertyType.Float, prop => (object)prop.floatValue },
			{ SerializedPropertyType.Integer, prop => (object)prop.intValue },
			{ SerializedPropertyType.LayerMask, prop => (object)(LayerMask)prop.intValue },
			{ SerializedPropertyType.ObjectReference, prop => (object)prop.objectReferenceValue },
			{ SerializedPropertyType.Quaternion, prop => (object)prop.quaternionValue },
			{ SerializedPropertyType.Rect, prop => (object)prop.rectValue },
			{ SerializedPropertyType.String, prop => (object)prop.stringValue },
			{ SerializedPropertyType.Vector2, prop => (object)prop.vector2Value },
			{ SerializedPropertyType.Vector3, prop => (object)prop.vector3Value },
			{ SerializedPropertyType.Vector4, prop => (object)prop.vector4Value },
		};
		/// <summary>
		/// The value setters for different serialized property types.
		/// </summary>
		private static readonly Dictionary<SerializedPropertyType, System.Action<SerializedProperty, object>> s_ValueSetters =
			new Dictionary<SerializedPropertyType, System.Action<SerializedProperty, object>>()
		{
			{ SerializedPropertyType.AnimationCurve, (prop, val) => prop.animationCurveValue = (AnimationCurve)val },
			{ SerializedPropertyType.ArraySize, (prop, val) => prop.intValue = (int)val },
			{ SerializedPropertyType.Boolean, (prop, val) => prop.boolValue = (bool)val },
			{ SerializedPropertyType.Bounds, (prop, val) => prop.boundsValue = (Bounds)val },
			{ SerializedPropertyType.Character, (prop, val) => prop.intValue = (int)val },
			{ SerializedPropertyType.Color, (prop, val) => prop.colorValue = (Color)val },
			{ SerializedPropertyType.Enum, (prop, val) => prop.intValue = (int)val },
			{ SerializedPropertyType.Float, (prop, val) => prop.floatValue = (float)val },
			{ SerializedPropertyType.Integer, (prop, val) => prop.intValue = (int)val },
			{ SerializedPropertyType.LayerMask, (prop, val) => prop.intValue = (int)val },
			{ SerializedPropertyType.ObjectReference, (prop, val) => prop.objectReferenceValue = (Object)val },
			{ SerializedPropertyType.Quaternion, (prop, val) => prop.quaternionValue = (Quaternion)val },
			{ SerializedPropertyType.Rect, (prop, val) => prop.rectValue = (Rect)val },
			{ SerializedPropertyType.String, (prop, val) => prop.stringValue = (string)val },
			{ SerializedPropertyType.Vector2, (prop, val) => prop.vector2Value = (Vector2)val },
			{ SerializedPropertyType.Vector3, (prop, val) => prop.vector3Value = (Vector3)val },
			{ SerializedPropertyType.Vector4, (prop, val) => prop.vector4Value = (Vector4)val },
		};

		/// <summary>
		/// Initializes the <see cref="SerializedPropertyX"/> class.
		/// </summary>
		static SerializedPropertyX()
		{
			// get all of the GUIDrawer types
			List<System.Type> drawerTypes = ReflectionX.AllTypes.Where(t => t.IsSubclassOf(typeof(GUIDrawer))).ToList();
			// associate object/attribute types with their respective drawers
			List<CustomPropertyDrawer> customDrawerAttrs = new List<CustomPropertyDrawer>();
			foreach (System.Type drawerType in drawerTypes)
			{
				if (drawerType.GetCustomAttributes(customDrawerAttrs) > 0)
				{
					System.Type baseType =
						s_CustomPropertyDrawerTypeField.GetValue(customDrawerAttrs[0]) as System.Type;
					Dictionary<System.Type, System.Type> registrationTable =
						typeof(DecoratorDrawer).IsAssignableFrom(drawerType) ?
							s_DecoratorsForEachType : s_DrawersForEachType;
					if (!registrationTable.ContainsKey(baseType))
					{
						registrationTable.Add(baseType, drawerType);
					}
					else
					{
						registrationTable[baseType] = drawerType;
					}
					if ((bool)s_CustomPropertyDrawerUseForChildrenField.GetValue(customDrawerAttrs[0]))
					{
						foreach (System.Type type in ReflectionX.AllTypes)
						{
							if (!registrationTable.ContainsKey(type) && type.IsSubclassOf(baseType))
							{
								registrationTable.Add(type, drawerType);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Determines whether the two values are equal.
		/// </summary>
		/// <returns><see langword="true"/> if the two values are equal; otherwise<see langword="false"/>.</returns>
		/// <param name="oldValue">Old value.</param>
		/// <param name="newValue">New value.</param>
		/// <param name="type">Type.</param>
		private static bool AreValuesEqual(object oldValue, object newValue, SerializedPropertyType type)
		{
			if (oldValue == null)
			{
				return newValue == null;
			}
			else if (newValue == null)
			{
				return oldValue == null;
			}
			else if (newValue.GetType() != oldValue.GetType())
			{
				return false;
			}
			switch (type)
			{
			case SerializedPropertyType.AnimationCurve:
				AnimationCurve curve1 = oldValue as AnimationCurve;
				AnimationCurve curve2 = newValue as AnimationCurve;
				return curve1.postWrapMode == curve2.postWrapMode &&
					curve1.preWrapMode == curve2.preWrapMode &&
					curve1.keys.SequenceEqual(curve2.keys);
			case SerializedPropertyType.Generic:
				System.Type fieldType = oldValue.GetType();
				if (typeof(IList).IsAssignableFrom(fieldType)) // compare list contents
				{
					IList list1 = oldValue as IList;
					IList list2 = newValue as IList;
					if (list1.Count != list2.Count)
					{
						return false;
					}
					if (list1.Count == 0)
					{
						return true;
					}
					System.Type elementType = GetIListElementType(list1.GetType());
					SerializedPropertyType elementPropertyType = SerializedPropertyType.Generic;
					if (s_SerializableTypes.ContainsKey(elementType))
					{
						elementPropertyType = s_SerializableTypes[elementType];
					}
					else if (elementType.IsEnum)
					{
						elementPropertyType = SerializedPropertyType.Enum;
					}
					else if (typeof(Object).IsAssignableFrom(elementType))
					{
						elementPropertyType = SerializedPropertyType.ObjectReference;
					}
					for (int elementIndex = 0; elementIndex < list1.Count; ++elementIndex)
					{
						if (!AreValuesEqual(list1[elementIndex], list2[elementIndex], elementPropertyType))
						{
							return false;
						}
					}
					return true;
				}
				else // compare built-in struct or IPropertyBackingFieldCompatible
				{
					System.Type objectType = oldValue.GetType();
					if (ReflectionX.UnityRuntimeAssemblies.Contains(objectType.Assembly))
					{
						if (!objectType.IsValueType)
						{
							LogIncompatibleUnityType(objectType);
							return false;
						}
						return newValue.Equals(oldValue);
					}
					if (!typeof(IPropertyBackingFieldCompatible).IsAssignableFrom(fieldType))
					{
						throw new System.ArgumentException(
							string.Format(
								"Supplied objects must impelement <b>{0}<{1}></b>.",
								typeof(IPropertyBackingFieldCompatible).FullName, objectType.FullName
							)
						);
					}
					IPropertyBackingFieldCompatible object1 = oldValue as IPropertyBackingFieldCompatible;
					IPropertyBackingFieldCompatible object2 = newValue as IPropertyBackingFieldCompatible;
					return object1.GetSerializedPropertiesHash() == object2.GetSerializedPropertiesHash();
				}
			default:
				return oldValue.Equals(newValue);
			}
		}

		/// <summary>
		/// Gets a copy of the value for the specified array serialized property for the target, converted to the
		/// specified IList type. This method is used to blindly convert between List<T> and T[] when required.
		/// </summary>
		/// <returns>The supplied IList value, converted to the specified IList type.</returns>
		/// <param name="arrayPropertyValue">Array property value.</param>
		/// <param name="targetType">Target type, either List<T> or T[].</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown if a supplied property value or targetType is not List<T> or T[].
		/// </exception>
		private static object GetConvertedIListValues(object arrayPropertyValue, System.Type targetType)
		{
			if (!typeof(IList).IsAssignableFrom(targetType))
			{
				throw new System.ArgumentException("Target type must be either T[] or List<T>", "targetType");
			}
			bool shouldBeArray = typeof(System.Array).IsAssignableFrom(targetType);
			System.Type elementType = GetIListElementType(targetType);
			if (elementType.IsGenericParameter)
			{
				// TODO: this may not be correct for a generic class with multiple arguments
				elementType = arrayPropertyValue.GetType().GetGenericArguments().FirstOrDefault();
			}
			System.Type listType = typeof(List<>).MakeGenericType(new System.Type[] { elementType });
			// convert the IList if it is not already of the correct type
			if (arrayPropertyValue.GetType() == targetType)
			{
				return arrayPropertyValue;
			}
			// populate a new list representation
			IList newList = (IList)System.Activator.CreateInstance(listType);
			if (arrayPropertyValue != null)
			{
				foreach (object element in (IEnumerable)arrayPropertyValue)
				{
					newList.Add(element);
				}
			}
			// convert to array if value should be an array
			if (shouldBeArray)
			{
				System.Array newArray = System.Array.CreateInstance(elementType, newList.Count);
				newList.CopyTo(newArray, 0);
				return newArray;
			}
			// other update value to list representation
			else
			{
				return newList;
			}
		}

		/// <summary>
		/// Gets a copy of the value for the specified array serialized property for the target, converted to the
		/// specified IList type. This method allows you to get T[] from a List<T> property, and vice versa.
		/// </summary>
		/// <returns>The value of the IList property, converted to the specified IList type.</returns>
		/// <param name="property">A serialized property of type List<T> or T[].</param>
		/// <param name="targetType">Target type, either List<T> or T[].</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown if a supplied property is not List<T> or T[].
		/// </exception>
		public static object GetConvertedIListValues(
			this SerializedProperty property, System.Type targetType
		)
		{
			if (
				!property.isArray ||
				property.propertyType == SerializedPropertyType.String ||
				property.propertyPath.EndsWith(".Array")
			)
			{
				throw new System.ArgumentException("Supplied property must be T[] or List<T>.", "property");
			}
			object arrayPropertyValue = property.GetValue();
			return GetConvertedIListValues(arrayPropertyValue, targetType);
		}

		/// <summary>
		/// Gets the index of the supplied property if it is an array element.
		/// </summary>
		/// <returns>Gets the index of the supplied property if it is an array element; otherwise -1.</returns>
		/// <param name="property">Property.</param>
		public static int GetElementIndex(this SerializedProperty property)
		{
			return s_MatchTerminalArrayElement.IsMatch(property.propertyPath) ?
				int.Parse(s_MatchTerminalArrayElement.Match(property.propertyPath).Groups["index"].Value) : -1;
		}

		/// <summary>
		/// Gets the GUI drawer for the supplied field.
		/// </summary>
		/// <returns>The GUI drawer for the supplied field.</returns>
		/// <param name="field">Field.</param>
		/// <param name="propertyAttribute">
		/// Property attribute. If null, this method will return a default drawer for the field type, if one exists.
		/// </param>
		public static GUIDrawer GetGUIDrawer(FieldInfo field, PropertyAttribute propertyAttribute)
		{
			GUIDrawer result = null;
			if (field == null)
			{
				return result;
			}
			System.Type fieldType = GetIListElementType(field.FieldType) ?? field.FieldType;
			List<Dictionary<System.Type, System.Type>> registrationTables =
				new List<Dictionary<System.Type, System.Type>>(
					new Dictionary<System.Type, System.Type>[] { s_DecoratorsForEachType, s_DrawersForEachType }
				);
			System.Type propertyAttributeType = propertyAttribute == null ? null : propertyAttribute.GetType();
			foreach (Dictionary<System.Type, System.Type> registrationTable in registrationTables)
			{
				// skip if there is no GUIDrawer for this type in the registration table
				if (
					!(
						registrationTable.ContainsKey(fieldType) ||
						(propertyAttributeType != null && registrationTable.ContainsKey(propertyAttributeType))
					)
				)
				{
					continue;
				}
				// instantiate the new GUIDrawer
				bool isTypeDrawer = registrationTable.ContainsKey(fieldType);
				if (isTypeDrawer && propertyAttributeType != null)
				{
					isTypeDrawer = !registrationTable.ContainsKey(propertyAttributeType);
				}
				System.Type typeKey = isTypeDrawer ? fieldType : propertyAttributeType;
				if (registrationTable.ContainsKey(typeKey) && !registrationTable[typeKey].IsAbstract)
				{
					ConstructorInfo constructor = registrationTable[typeKey].GetConstructor(new System.Type[0]);
					result = constructor.Invoke(null) as GUIDrawer;
				}
				else
				{
					return null;
				}
				// configure the drawer's private fields
				if (result is PropertyDrawer)
				{
					if (isTypeDrawer)
					{
						s_PropertyDrawerAttributeField.SetValue(result, null);
					}
					else
					{
						s_PropertyDrawerAttributeField.SetValue(result, propertyAttribute);
					}
					s_PropertyDrawerFieldInfoField.SetValue(result, field);
				}
				else if (result is GUIDrawer)
				{
					s_DecoratorDrawerAttributeField.SetValue(result, propertyAttribute);
				}
			}
			return result;
		}
		
		/// <summary>
		/// Gets the type of the IList element.
		/// </summary>
		/// <returns>
		/// The IList element type if the supplied type is assignable from IList, otherwise <see langword="null"/>.
		/// </returns>
		/// <param name="type">An IList type.</param>
		public static System.Type GetIListElementType(System.Type type)
		{
			if (typeof(IList).IsAssignableFrom(type))
			{
				if (type.IsArray)
				{
					return type.GetElementType();
				}
				else
				{
					return type.GetGenericArguments()[0];
				}
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the instance field for a serialized property, which may be defined in a base class.
		/// </summary>
		/// <returns>The instance field for a serialized property.</returns>
		/// <param name="type">Type.</param>
		/// <param name="fieldName">Field name.</param>
		private static FieldInfo GetInstanceFieldForSerializedProperty(System.Type type, string fieldName)
		{
			FieldInfo result = type.GetInstanceField(fieldName);
			if (result == null)
			{
				System.Type t = type;
				while (t.BaseType != null)
				{
					result = t.BaseType.GetInstanceField(fieldName);
					if (result != null)
					{
						break;
					}
					t = t.BaseType;
				}
			}
			return result;
		}

		/// <summary>
		/// Gets the last property defined on <paramref name="target"/> with the specified <paramref name="baseType"/>.
		/// </summary>
		/// <returns>
		/// The last property defined on <paramref name="target"/> with the specified <paramref name="baseType"/>.
		/// </returns>
		/// <param name="target">Target.</param>
		/// <param name="baseType">Base type.</param>
		private static SerializedProperty GetLastProperty(SerializedObject target, System.Type baseType)
		{
			FieldInfo lastField = GetLastSerializableField(baseType);
			if (lastField == null || target == null || !baseType.IsAssignableFrom(target.targetObject.GetType()))
			{
				return null;
			}
			return target.FindProperty(lastField.Name);
		}

		/// <summary>
		/// Gets the last serializable field defined for the specified type.
		/// </summary>
		/// <returns>The last serializable field defined for the specified type.</returns>
		/// <param name="type">Type.</param>
		private static FieldInfo GetLastSerializableField(System.Type type)
		{
			if (type == null)
			{
				return null;
			}
			if (!s_LastSerializableFields.ContainsKey(type))
			{
				s_LastSerializableFields[type] = null;
				foreach (
					FieldInfo fieldInfo in
					type.GetFields(ReflectionX.instanceBindingFlags).Where(f => IsFieldSerializable(f))
				)
				{
					if (s_LastSerializableFields[type] != null && fieldInfo.DeclaringType != type)
					{
						break;
					}
					s_LastSerializableFields[type] = fieldInfo;
				}
			}
			return s_LastSerializableFields[type];
		}

		/// <summary>
		/// Gets the parent property if the supplied property is a child.
		/// </summary>
		/// <returns>
		/// The parent property if the supplied property is a child; otherwise, <see langword="null"/>. If the supplied
		/// property is an array element or array size, then the property returned is m_Property, not m_Property.Array.
		/// </returns>
		/// <param name="property">Property.</param>
		public static SerializedProperty GetParentProperty(this SerializedProperty property)
		{
			return property.propertyPath.Contains('.') ?
				property.serializedObject.FindProperty(
					property.propertyPath.Range(
						0,
						property.IsArraySize() ?
							-11 : // - ".Array.size".Length
							property.IsArrayElement() ? -1 * (
								s_MatchTerminalArrayElement.Match(property.propertyPath).Value.Length + 11 // ".Array.data".Length
							) :
						property.propertyPath.LastIndexOf('.') - property.propertyPath.Length
					)
				) : null;
		}

		/// <summary>
		/// Gets the parent property path if the supplied property path is a child.
		/// </summary>
		/// <returns>
		/// The parent property if the supplied property is a child; otherwise, <see langword="null"/>. If the supplied
		/// property is an array element or array size, then the property returned is m_Property, not m_Property.Array.
		/// </returns>
		/// <param name="property">Property.</param>
		public static string GetParentPropertyPath(string propertyPath)
		{
			return propertyPath.Contains('.') ?
				propertyPath.Range(
					0,
					s_MatchTerminalArraySize.IsMatch(propertyPath) ?
						-11 : // - ".Array.size".Length
						IsArrayElementPath(propertyPath) ? -1 * (
							s_MatchTerminalArrayElement.Match(propertyPath).Value.Length + 11 // ".Array.data".Length
						) : propertyPath.LastIndexOf('.') - propertyPath.Length
				) : null;
		}
		
		/// <summary>
		/// Gets the property drawer for the specified serialized property.
		/// </summary>
		/// <returns>The property drawer for the specified serialized property.</returns>
		/// <param name="property">Property.</param>
		public static PropertyDrawer GetPropertyDrawer(this SerializedProperty property)
		{
			FieldInfo field;
			property.GetProvider(out field);
			PropertyAttribute propertyAttribute = null;
			using (var propertyAttrs = new ListPool<PropertyAttribute>.Scope())
			{
				for (int i = 0; i < field.GetCustomAttributes(propertyAttrs.List, true); ++i)
				{
					if (s_DecoratorsForEachType.ContainsKey(propertyAttrs.List[i].GetType()))
					{
						continue;
					}
					if (propertyAttribute != null)
					{
						Debug.LogWarning(
							string.Format("Found multiple PropertyAttributes specified for {0}", property.propertyPath)
						);
					}
					if (propertyAttrs.List[i] is PropertyBackingFieldAttribute)
					{
						propertyAttribute = (propertyAttrs.List[i] as PropertyBackingFieldAttribute).OverrideAttribute;
					}
					else
					{
						propertyAttribute = propertyAttrs.List[i];
					}
				}
			}
			return GetPropertyDrawer(field, propertyAttribute);
		}
		
		/// <summary>
		/// Gets the property drawer for the supplied field.
		/// </summary>
		/// <returns>The property drawer for the supplied field.</returns>
		/// <param name="field">Field.</param>
		/// <param name="propertyAttribute">
		/// Property attribute. If null, this method will return a default drawer for the field type, if one exists.
		/// </param>
		public static PropertyDrawer GetPropertyDrawer(FieldInfo field, PropertyAttribute propertyAttribute)
		{
			return GetGUIDrawer(field, propertyAttribute) as PropertyDrawer; 
		}

		/// <summary>
		/// Gets the provider for the specified property. For example m_SomeProperty.m_SomeObject.m_SomeField returns
		/// the current value of m_SomeObject on the target object.
		/// </summary>
		/// <returns>The provider for the specified serialized property, to then use with reflection.</returns>
		/// <param name="property">A serialized property.</param>
		/// <param name="fieldInfo">The FieldInfo associated with the property.</param>
		public static object GetProvider(this SerializedProperty property, out FieldInfo fieldInfo)
		{
			object provider = property.serializedObject.targetObject;
			System.Type providerType = property.serializedObject.targetObject.GetType();
			fieldInfo = GetInstanceFieldForSerializedProperty(providerType, property.propertyPath.Split('.')[0]);
			string patchedPropertyPath = s_MatchTerminalProperty.Replace(property.propertyPath, "");
			patchedPropertyPath = s_MatchArrayElement.Replace(
				patchedPropertyPath, match => string.Format("[{0}]", match.Groups["index"].Value)
			);
			// proceed to search for provider if path is still nested
			if (!string.IsNullOrEmpty(patchedPropertyPath))
			{
				foreach (string part in patchedPropertyPath.Split('.'))
				{
					if (s_MatchTerminalArrayElement.IsMatch(part))
					{
						fieldInfo = GetInstanceFieldForSerializedProperty(
							providerType, s_MatchTerminalArrayElement.Replace(part, "")
						);
						int elementIndex = int.Parse(s_MatchTerminalArrayElement.Match(part).Groups["index"].Value);
						providerType = GetIListElementType(fieldInfo.FieldType);
						if (provider != null)
						{
							IList listValue = fieldInfo.GetValue(provider) as IList;
							if (listValue.Count <= elementIndex)
							{
								provider = null;
							}
							else
							{
								provider = listValue[elementIndex];
							}
						}
					}
					else
					{
						fieldInfo = GetInstanceFieldForSerializedProperty(providerType, part);
						provider = fieldInfo.GetValue(provider);
						providerType = provider.GetType();
					}
				}
				// ensure field info is properly set
				fieldInfo = GetInstanceFieldForSerializedProperty(
					providerType, s_MatchTerminalProperty.Match(property.propertyPath).Groups["name"].Value
				);
			}
			return provider;
		}

		/// <summary>
		/// Gets the remaining visible properties on <paramref name="target"/> following the final
		/// <see cref="UnityEditor.SerializedProperty"/> defined in the base type specified by <typeparamref name="T">.
		/// </summary>
		/// <returns>The number of remaining visible properties.</returns>
		/// <param name="target">Target.</param>
		/// <param name="remainingVisibleProperties">List of remaining visible properties to populate.</param>
		/// <typeparam name="T">The base <see cref="UnityEngine.Object"/> type.</typeparam>
		public static int GetRemainingVisibleProperties<T>(
			SerializedObject serializedTarget, List<SerializedProperty> remainingVisibleProperties
		) where T : Object
		{
			remainingVisibleProperties.Clear();
			SerializedProperty sp = GetLastProperty(serializedTarget, typeof(T));
			if (sp != null)
			{
				while (sp.NextVisible(false))
				{
					remainingVisibleProperties.Add(sp.Copy());
				}
			}
			return remainingVisibleProperties.Count;
		}

		/// <summary>
		/// Gets a copy of the value for the specified serialized property.
		/// </summary>
		/// <returns>A copy of the value for the specified serialized property.</returns>
		/// <param name="property">Property.</param>
		/// <param name="getEnumAsInt">
		/// If <see langword="true"/> then return an enum as a <see cref="System.Int32"/>; otherwise, return it as its
		/// native type. Returning an int is much faster, but does no type checking.
		/// </param>
		public static object GetValue(this SerializedProperty property, bool getEnumAsInt = false)
		{
			object value = null;
			// if it is a simple/known type, then use the built-in accessors to retrieve pending value
			if (
				s_ValueGetters.ContainsKey(property.propertyType) &&
				(getEnumAsInt || property.propertyType != SerializedPropertyType.Enum)
			)
			{
				value = s_ValueGetters[property.propertyType](property);
				return value;
			}
			FieldInfo field;
			if (property.propertyType == SerializedPropertyType.Enum)
			{
				property.GetProvider(out field);
				System.Type fieldType =
					property.IsArrayElement() ? GetIListElementType(field.FieldType): field.FieldType;
				return System.Enum.ToObject(fieldType, property.intValue);
			}
			// if it is SerializedPropertyType.Generic, then create pending value representation
			object provider = property.GetProvider(out field);
			if (provider == null)
			{
				return null;
			}
			// for an array or list, just populate each element with its pending value
			if (property.isArray)
			{
				value = (
					from i in Enumerable.Range(0, property.arraySize)
					select property.GetArrayElementAtIndex(i).GetValue()
				);
				value = GetConvertedIListValues(value, field.FieldType);
			}
			// for classes and structs, create a clone and copy serialized fields
			else
			{
				int elementIndex = property.GetElementIndex();
				System.Type elementType = elementIndex >= 0 ? GetIListElementType(field.FieldType) : field.FieldType;
				// only value types and ICloneable can be queried using this method
				if (!elementType.IsValueType && !typeof(System.ICloneable).IsAssignableFrom(elementType))
				{
					if (ReflectionX.UnityRuntimeAssemblies.Contains(elementType.Assembly))
					{
						LogIncompatibleUnityType(elementType);
						return value;
					}
					else
					{
						Debug.LogException(
							new System.ArgumentException(
								string.Format(
									"The custom serializable class <b>{0}</b> must implement <b>{1}</b> in order to " +
									"be queried from a SerializedProperty. <i>({2}.{3})</i>",
									elementType.FullName,
									typeof(System.ICloneable).FullName,
									property.serializedObject.targetObject.GetType().FullName,
									s_MatchArrayElement.Replace(property.propertyPath, "[]")
								)
							), property.serializedObject.targetObject
						);
						return value;
					}
				}
				object clone = null;
				IList sequence = null;
				if (!s_FieldsToCopyForEachType.ContainsKey(elementType))
				{
					s_FieldsToCopyForEachType.Add(
						elementType,
						new ReadOnlyCollection<FieldInfo>(
							(
								from f in elementType.GetFields(ReflectionX.instanceBindingFlags) select f
							).Where(f => IsFieldCopyable(f)).ToArray()
						)
					);
				}
				clone = field.GetValue(provider);
				if (elementIndex >= 0)
				{
					sequence = clone as IList;
					// NOTE: Unity duplicates final element when new one is added
					clone = sequence.Count > elementIndex ?
						sequence[elementIndex >= sequence.Count ? sequence.Count - 1 : elementIndex] : null;
				}
				if (!elementType.IsValueType)
				{
					clone = clone == null ? null : ((System.ICloneable)clone).Clone();
				}
				System.Type cloneType = clone == null ? null : clone.GetType();
				for (int i = 0; i < s_FieldsToCopyForEachType[elementType].Count; ++i)
				{
					FieldInfo fi = s_FieldsToCopyForEachType[elementType][i];
					if (cloneType != null && fi.DeclaringType.IsSubclassOf(cloneType))
					{
						Debug.LogException(
							new System.MissingFieldException(
								string.Format(
									"Expected provider of type {0} or a subclass but found {1}. Did you forget to override Clone() in {0}?\n",
									fi.DeclaringType, cloneType
								)
							)
						);
						continue;
					}
					s_FieldsToCopyForEachType[elementType][i].SetValue(
						clone, property.FindPropertyRelative(s_FieldsToCopyForEachType[elementType][i].Name).GetValue()
					);
				}
				value = clone;
			}
			return value;
		}
		
		/// <summary>
		/// Gets a copy of the value for the specified serialized property.
		/// </summary>
		/// <returns>A copy of the value for the specified serialized property.</returns>
		/// <param name="property">Property.</param>
		/// <typeparam name="T">
		/// The type of the serialized property. If it is a custom class or struct, it must implement
		/// Candlelight.IPropertyBackingFieldCompatible.
		/// </typeparam>
		public static T GetValue<T>(this SerializedProperty property)
		{
			object value = GetValue(property);
			return value == null ? default(T) : (T)value;
		}

		/// <summary>
		/// Determines if the specified <see cref="UnityEngine.Object"/> type has a serialized field with the
		/// specified <paramref name="propertyPath"/>.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified <see cref="UnityEngine.Object"/> type has a serialized field with
		/// the specified <paramref name="propertyPath"/>; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="targetType">A <see cref="UnityEngine.Object"/> type.</param>
		/// <param name="propertyPath">Property path.</param>
		public static bool HasSerializedProperty(this System.Type targetType, string propertyPath)
		{
			if (string.IsNullOrEmpty(propertyPath))
			{
				return false;
			}
			if (
				propertyPath == "m_Script" && (
					typeof(MonoBehaviour).IsAssignableFrom(targetType) ||
					typeof(ScriptableObject).IsAssignableFrom(targetType)
				)
			)
			{
				return true;
			}
			System.Type providerType = targetType;
			foreach (string token in propertyPath.Split('.'))
			{
				FieldInfo field = GetInstanceFieldForSerializedProperty(providerType, token);
				if (field == null || !IsFieldSerializable(field))
				{
					return false;
				}
				providerType = field.FieldType;
			}
			return true;
		}

		/// <summary>
		/// Determines if the specified property is an array element.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified property is an array element; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="property">Property.</param>
		public static bool IsArrayElement(this SerializedProperty property)
		{
			return IsArrayElementPath(property.propertyPath);
		}

		/// <summary>
		/// Determines if the specified property path is an array element path.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified property path is an array element path; otherwise,
		/// <see langword="false"/>.
		/// </returns>
		/// <param name="propertyPath">Property path.</param>
		public static bool IsArrayElementPath(string propertyPath)
		{
			return s_MatchTerminalArrayElement.IsMatch(propertyPath);
		}

		/// <summary>
		/// Determines if the specified property is an array size.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified property is an array size; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="property">Property.</param>
		public static bool IsArraySize(this SerializedProperty property)
		{
			if (!s_MatchTerminalArraySize.IsMatch(property.propertyPath))
			{
				return false;
			}
			SerializedProperty parent =
				property.serializedObject.FindProperty(property.propertyPath.Range(0, -11)); // - ".Array.size".Length
			return parent.isArray;
		}

		/// <summary>
		/// Determines whether the supplied element is unique in the supplied list.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the element is unique in the supplied array property; otherwise,
		/// <see langword="false"/>.
		/// </returns>
		/// <param name="arrayProperty">Array property.</param>
		/// <param name="elementValue">Element value being tested.</param>
		/// <param name="identifierPropertyPath">
		/// Optional value to specify an identifier property path relative to each element property.
		/// </param>
		public static bool IsElementUnique(
			this SerializedProperty arrayProperty, object elementValue, string identifierPropertyPath = ""
		)
		{
			if (arrayProperty.arraySize == 0)
			{
				return false;
			}
			SerializedPropertyType propertyType;
			if (string.IsNullOrEmpty(identifierPropertyPath))
			{
				propertyType = arrayProperty.GetArrayElementAtIndex(0).propertyType;
				return (
					from elementIndex in Enumerable.Range(0, arrayProperty.arraySize)
					select arrayProperty.GetArrayElementAtIndex(elementIndex).GetValue()
				).Count(id => AreValuesEqual(id, elementValue, propertyType)) == 1;
			}
			else
			{
				propertyType =
					arrayProperty.GetArrayElementAtIndex(0).FindPropertyRelative(identifierPropertyPath).propertyType;
				HashSet<object> identifiers = new HashSet<object>();
				SerializedProperty sp;
				object val;
				for (int i = 0; i < arrayProperty.arraySize; ++i)
				{
					// BUG: multi-selected array properties may iterate into a following property...
					sp = arrayProperty.GetArrayElementAtIndex(i);
					if (sp == null)
					{
						continue;
					}
					sp = sp.FindPropertyRelative(identifierPropertyPath);
					if (sp == null)
					{
						continue;
					}
					val = sp.GetValue();
					if (identifiers.Contains(val))
					{
						return false;
					}
					identifiers.Add(val);
				}
				return true;
			}
		}

		/// <summary>
		/// Determines if the specified field is copyable.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified field is copyable; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="field">Field.</param>
		private static bool IsFieldCopyable(FieldInfo field)
		{
			if (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Count() > 0)
			{
				if (!IsTypeSerializable(field.FieldType))
				{
					return false;
				}
				if (field.FieldType.IsClass)
				{
					return typeof(IPropertyBackingFieldCompatible).IsAssignableFrom(field.FieldType);
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Determines if the specified field is convertable into a <see cref="UnityEditor.SerializedProperty"/>.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> the specified field is convertable into a
		/// <see cref="UnityEditor.SerializedProperty"/>; otherwise, <see cref="false"/>.
		/// </returns>
		/// <param name="field">Field.</param>
		private static bool IsFieldSerializable(FieldInfo field)
		{
			if (field == null)
			{
				return false;
			}
			if (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Count() > 0)
			{
				if (!IsTypeSerializable(field.FieldType))
				{
					return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Determines if the specified type is serializable.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the specified type is serializable; otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="type">Type.</param>
		private static bool IsTypeSerializable(System.Type type)
		{
			if (
				s_SerializableTypes.ContainsKey(type) ||
				type.IsEnum ||
				typeof(Object).IsAssignableFrom(type)
			)
			{
				return true;
			}
			if (typeof(IList).IsAssignableFrom(type))
			{
				return IsTypeSerializable(GetIListElementType(type));
			}
			using (var serializableAttrs = new ListPool<System.SerializableAttribute>.Scope())
			{
				return type.GetCustomAttributes(serializableAttrs.List, true) > 0;
			}
		}
		
		/// <summary>
		/// Determines if the supplied property's current value is equal to the specified other value.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the supplied property's current value is equal to the specified other value;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		/// <param name="property">Property.</param>
		/// <param name="other">Other.</param>
		public static bool IsValueEqualTo(this SerializedProperty property, object other)
		{
			return AreValuesEqual(other, property.GetValue(), property.propertyType);
		}

		/// <summary>
		/// Display an exception when an incompatible built-in Unity type is encountered.
		/// </summary>
		/// <param name="type">Type.</param>
		private static void LogIncompatibleUnityType(System.Type type)
		{
			Debug.LogException(
				new System.ArgumentException(
					string.Format(
						"The built-in serializable type <b>{0}</b> is not supported for use with this feature. " +
						"Please file a bug report from Preferences -> Candlelight -> Property Backing Field",
						type.FullName
					)
				)
			);
		}

		/// <summary>
		/// Sets the value of the property.
		/// </summary>
		/// <param name="property">Property.</param>
		/// <param name="value">Value assumed to be of the right type.</param>
		public static void SetValue(this SerializedProperty property, object value)
		{
			if (s_ValueSetters.ContainsKey(property.propertyType))
			{
				s_ValueSetters[property.propertyType](property, value);
			}
			else
			{
				Debug.LogError(string.Format("Unsupported serialized property type: {0}", property.propertyType));
			}
		}
	}
}