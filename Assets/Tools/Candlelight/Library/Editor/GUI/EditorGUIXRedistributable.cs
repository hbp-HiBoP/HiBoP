// 
// EditorGUIXRedistributable.cs
// 
// Copyright (c) 2012-2016, Candlelight Interactive, LLC
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
// This file contains a redistributable part of a static class for working
// with editor GUI.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Candlelight
{
	/// <summary>
	/// Editor GUI extensions.
	/// </summary>
	public static partial class EditorGUIX
	{
		/// <summary>
		/// The on/off labels.
		/// </summary>
		private static readonly GUIContent[] s_OnOffLabels =
			new GUIContent[] { new GUIContent("Off"), new GUIContent("On") };

		#region Backing Fields
		private const float k_NarrowButtonWidth = 48f;
		private const float k_WideButtonWidth = 80f;
		#endregion

		/// <summary>
		/// The margin between controls.
		/// </summary>
		public static RectOffset ControlMargin
		{
			get
			{
				return new RectOffset(
					4, 4, (int)EditorGUIUtility.standardVerticalSpacing, (int)EditorGUIUtility.standardVerticalSpacing
				);
			}
		}
		/// <summary>
		/// Gets the height of the horizontal line.
		/// </summary>
		/// <value>The height of the horizontal line.</value>
		public static float HorizontalLineHeight { get { return 2f; } }
		/// <summary>
		/// Gets the height of an inline button.
		/// </summary>
		/// <value>The height of an inline button.</value>
		public static float InlineButtonHeight
		{
			get { return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; }
		}
		/// <summary>
		/// Gets the standard horizontal spacing.
		/// </summary>
		/// <value>The standard horizontal spacing.</value>
		public static float StandardHorizontalSpacing { get { return 0.5f * ControlMargin.horizontal; } }
		/// <summary>
		/// Gets the tinted color of the GUI based on play mode state.
		/// </summary>
		/// <value>The tinted color of the GUI based on play mode state.</value>
		private static Color TintedGUIColor
		{
			get { return GUI.color * (Application.isPlaying ? new Color(0.85f, 0.85f, 0.85f, 1f) : Color.white); }
		}

		/// <summary>
		/// Create a button in the editor GUI layout.
		/// </summary>
		/// <returns><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</returns>
		/// <param name="label">Label.</param>
		/// <param name="style">Optional style override.</param>
		/// <param name="isActive">True if the button should display the active state.</param>
		/// <param name="controlId">
		/// Control identifier. If left unspecified, then the text of the label will be used to generate one.
		/// </param>
		public static bool DisplayButton(string label, GUIStyle style = null, bool isActive = false, int controlId = 0)
		{
			return DisplayButton(new GUIContent(label), style, isActive, controlId);
		}
		
		/// <summary>
		/// Create a button in the editor GUI layout.
		/// </summary>
		/// <returns><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</returns>
		/// <param name="label">Label.</param>
		/// <param name="style">Optional style override.</param>
		/// <param name="isActive">True if the button should display the active state.</param>
		/// <param name="controlId">
		/// Control identifier. If left unspecified, then the text of the label will be used to generate one.
		/// </param>
		public static bool DisplayButton(
			GUIContent label, GUIStyle style = null, bool isActive = false, int controlId = 0
		)
		{
			Rect position =
				GUILayoutUtility.GetRect(0f, EditorGUIX.InlineButtonHeight + EditorGUIUtility.standardVerticalSpacing);
			position = EditorGUI.IndentedRect(position);
			position.height -= EditorGUIUtility.standardVerticalSpacing;
			return DisplayButton(position, label, style, isActive, controlId);
		}
		
		/// <summary>
		/// Create a button in the editor GUI.
		/// </summary>
		/// <returns><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</returns>
		/// <param name="position">Position.</param>
		/// <param name="label">Label.</param>
		/// <param name="style">Optional style override.</param>
		/// <param name="isActive">True if the button should display the active state.</param>
		/// <param name="controlId">
		/// Control identifier. If left unspecified, then the text of the label will be used to generate one.
		/// </param>
		public static bool DisplayButton(
			Rect position, string label, GUIStyle style = null, bool isActive = false, int controlId = 0
		)
		{
			return DisplayButton(position, new GUIContent(label), style, isActive, controlId);
		}
		
		/// <summary>
		/// Create a button in the editor GUI.
		/// </summary>
		/// <returns><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</returns>
		/// <param name="position">Position.</param>
		/// <param name="label">Label.</param>
		/// <param name="style">Optional style override.</param>
		/// <param name="isActive">True if the button should display the active state.</param>
		/// <param name="controlId">
		/// Control identifier. If left unspecified, then the text of the label will be used to generate one.
		/// </param>
		public static bool DisplayButton(
			Rect position, GUIContent label, GUIStyle style = null, bool isActive = false, int controlId = 0
		)
		{
			
			Color oldColor = GUI.color;
			GUI.color = TintedGUIColor;
			bool result = DisplayEditorButton(position, label, style, isActive, controlId);
			GUI.color = oldColor;
			return result;
		}

		/// <summary>
		/// Displays an editor button.
		/// </summary>
		/// <returns><see langword="true"/> if editor button is pressed; otherwise, <see langword="false"/>.</returns>
		/// <param name="position">Position.</param>
		/// <param name="label">Label.</param>
		/// <param name="style">Options_InlineButtonHeight</param>
		/// <param name="isActive">True if the button should display the active state.</param>
		/// <param name="controlId">
		/// Control identifier. If left unspecified, then the text of the label will be used to generate one.
		/// </param>
		private static bool DisplayEditorButton(
			Rect position, GUIContent label, GUIStyle style, bool isActive, int controlId = 0
		)
		{
			controlId = GUIUtility.GetControlID(
				controlId == 0 ? label.text.GetHashCode() : controlId, FocusType.Keyboard, position
			);
			Event current = Event.current;
			if (
				GUIUtility.keyboardControl == controlId &&
				current.type == EventType.KeyDown && (
					current.keyCode == KeyCode.Space ||
					current.keyCode == KeyCode.KeypadEnter ||
					current.keyCode == KeyCode.Return
				)
			)
			{
				current.Use();
				GUI.changed = true;
			}
			if (
				current.type == EventType.MouseDown &&
				Event.current.button == 0 &&
				position.Contains(Event.current.mousePosition)
			)
			{
				GUIUtility.keyboardControl = controlId;
				EditorGUIUtility.editingTextField = false;
				HandleUtility.Repaint();
			}
			EditorGUI.BeginChangeCheck();
			GUI.Toggle(position, controlId, isActive, label, style == null ? EditorStyles.miniButton : style);
			return EditorGUI.EndChangeCheck();
		}

		/// <summary>
		/// Displays a horizontal line in the current layout.
		/// </summary>
		public static void DisplayHorizontalLine()
		{
			GUI.Box(
				EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true, EditorGUIX.HorizontalLineHeight)),
				GUIContent.none
			);
		}

		/// <summary>
		/// Displays the on/off toggle.
		/// </summary>
		/// <returns>The value of the on/off toggle.</returns>
		/// <param name="label">Label.</param>
		/// <param name="val">Value.</param>
		public static bool DisplayOnOffToggle(GUIContent label, bool val)
		{
			Rect position = EditorGUILayout.GetControlRect(true);
			Rect controlPosition, buttonPosition;
			GetRectsForControlWithInlineButton(position, out controlPosition, out buttonPosition);
			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = controlPosition.width;
#if !UNITY_4_6 && !UNITY_4_7
			if (EditorStylesX.IsUsingBuiltinSkin)
			{
				EditorGUI.PrefixLabel(controlPosition, label);
			}
			else
			{
				EditorGUI.PrefixLabel(controlPosition, label, GUI.skin.label);
			}
#else
			EditorGUI.PrefixLabel(controlPosition, label);
#endif
			EditorGUIUtility.labelWidth = oldLabelWidth;
			return DisplaySelectionGrid(buttonPosition, val ? 1 : 0, s_OnOffLabels, 2) == 1;
		}

		/// <summary>
		/// Displays a selection grid in the current layout.
		/// </summary>
		/// <returns>The currently selected index.</returns>
		/// <param name="currentIndex">The currently selected index.</param>
		/// <param name="labels">Labels.</param>
		/// <param name="xCount">Number of buttons in each grid row.</param>
		/// <param name="style">Optional style override.</param>
		public static int DisplaySelectionGrid(
			int currentIndex, GUIContent[] labels, int xCount = 0, GUIStyle style = null
		)
		{
			xCount = Mathf.Min(xCount < 1 ? labels.Length : xCount, labels.Length);
			int numRows = xCount > 0 ? labels.Length / xCount + (labels.Length % xCount == 0 ? 0 : 1) : 1;
			float ctrlHeight =
				style == null || style.fixedHeight == 0f ? EditorGUIUtility.singleLineHeight : style.fixedHeight;
			float ctrlSpacing = style == null ? EditorGUIUtility.standardVerticalSpacing : style.margin.vertical;
			Rect position =
				EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0f, (ctrlHeight + ctrlSpacing) * numRows));
			return DisplaySelectionGrid(position, currentIndex, labels, xCount, style);
		}

		/// <summary>
		/// Displays a selection grid.
		/// </summary>
		/// <returns>The currently selected index.</returns>
		/// <param name="position">Position.</param>
		/// <param name="currentIndex">The currently selected index.</param>
		/// <param name="labels">Labels.</param>
		/// <param name="xCount">Number of buttons in each grid row.</param>
		/// <param name="style">Optional style override.</param>
		public static int DisplaySelectionGrid(
			Rect position, int currentIndex, GUIContent[] labels, int xCount = 0, GUIStyle style = null
		)
		{
			xCount = Mathf.Min(xCount < 1 ? labels.Length : xCount, labels.Length);
			Color oldColor = GUI.color;
			GUI.color = TintedGUIColor;
			int numRows = xCount > 0 ? labels.Length / xCount + (labels.Length % xCount == 0 ? 0 : 1) : 1;
			float ctrlHeight =
				style == null || style.fixedHeight == 0f ? EditorGUIUtility.singleLineHeight : style.fixedHeight;
			float ctrlSpacing = style == null ? EditorGUIUtility.standardVerticalSpacing : style.margin.vertical;
			position.height = ctrlHeight;
			position.width /= xCount;
			Vector2 anchor = new Vector2(position.x, position.y);
			for (int row = 0; row < numRows; ++row)
			{
				position.y = anchor.y + (position.height + ctrlSpacing) * row;
				for (int col = 0; col < xCount; ++col)
				{
					position.x = anchor.x + position.width * col;
					int i = row * xCount + col;
					if (i < labels.Length && DisplayButton(position, labels[i], style, i == currentIndex))
				    {
						currentIndex = i;
					}
				}
			}
			GUI.color = oldColor;
			return currentIndex;
		}
		
		/// <summary>
		/// Displays a tab group.
		/// </summary>
		/// <returns>The current tab index.</returns>
		/// <param name="currentTab">Current tab index.</param>
		/// <param name="tabs">Tab labels.</param>
		/// <param name="tabContents">GUI callbacks to invoke for each tab.</param>
		/// <param name="xCount">Number of tabs to draw in each row.</param>
		public static int DisplayTabGroup(
			int currentTab, GUIContent[] tabs, Dictionary<int, System.Action> tabContents, int xCount = 0
		)
		{
			currentTab = DisplaySelectionGrid(currentTab, tabs, xCount, EditorStylesX.BrightTab);
			int indent = (int)(EditorGUI.IndentedRect(new Rect(0, 0, 1, 1)).x * 1.6f);
			EditorStylesX.TabAreaBackground.margin.left += indent;
			EditorGUILayout.BeginVertical(EditorStylesX.TabAreaBackground);
			{
				EditorStylesX.TabAreaBackground.margin.left -= indent;
				EditorGUILayout.Separator();
				int oldIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				if (tabContents.ContainsKey(currentTab) && tabContents[currentTab] != null)
				{
					tabContents[currentTab].Invoke();
				}
				else
				{
					EditorGUILayout.HelpBox(
						string.Format("No draw method supplied for tab {0}", currentTab), MessageType.Error
					);
				}
				EditorGUI.indentLevel = oldIndent;
			}
			EditorGUILayout.EndVertical();
			return currentTab;
		}

		/// <summary>
		/// Gets the rects for a control and an inline button next to it. You can specify desired button widths for wide
		/// and not wide modes, and the width will not exceed the field width.
		/// </summary>
		/// <param name="position">Position in which to draw.</param>
		/// <param name="controlRect">Control rect.</param>
		/// <param name="buttonRect">Button rect.</param>
		/// <param name="buttonNarrow">Button width to use in narrow mode.</param>
		/// <param name="buttonWide">Button width to use in wide mode.</param>
		public static void GetRectsForControlWithInlineButton(
			Rect position,
			out Rect controlRect,
			out Rect buttonRect,
			float buttonNarrow = k_WideButtonWidth,
			float buttonWide = k_WideButtonWidth
		)
		{
			float buttonWidth = Mathf.Min(
				EditorGUIUtility.wideMode ? buttonWide : buttonNarrow, position.width - EditorGUIUtility.labelWidth
			);
			controlRect = position;
			controlRect.width -= buttonWidth + StandardHorizontalSpacing;
			buttonRect = new Rect(
				controlRect.x + controlRect.width + StandardHorizontalSpacing,
				position.y,
				buttonWidth,
				EditorGUIUtility.singleLineHeight
			);
		}
	}
}