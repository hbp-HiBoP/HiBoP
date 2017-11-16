// 
// EulerAnglesDrawer.cs
// 
// Copyright (c) 2015, Candlelight Interactive, LLC
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
// This file contains a custom property drawer to display a quaternion field as
// Euler angles.

using UnityEditor;
using UnityEngine;

namespace Candlelight
{
	/// <summary>
	/// Euler angles drawer.
	/// </summary>
	[CustomPropertyDrawer(typeof(EulerAnglesAttribute))]
	public class EulerAnglesDrawer : PropertyDrawer
	{
		/// <summary>
		/// Gets the height of the property.
		/// </summary>
		/// <returns>The property height.</returns>
		/// <param name="property">Property.</param>
		/// <param name="label">Label.</param>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1f : 2f);
		}

		/// <summary>
		/// Raises the GUI event.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="property">Property.</param>
		/// <param name="label">Label.</param>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Vector3 euler = property.quaternionValue.eulerAngles;
			EditorGUI.BeginProperty(position, label, property);
			{
				EditorGUI.BeginChangeCheck();
				{
					euler = EditorGUI.Vector3Field(position, label, euler);
				}
				if (EditorGUI.EndChangeCheck())
				{
					property.quaternionValue = Quaternion.Euler(euler);
				}
			}
			EditorGUI.EndProperty();
		}
	}
}