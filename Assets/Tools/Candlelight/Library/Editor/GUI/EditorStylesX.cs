// 
// EditorStylesX.cs
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
// This file contains an extension class for accessing built-in editor styles.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Candlelight
{
	/// <summary>
	/// Editor styles extensions.
	/// </summary>
	public static class EditorStylesX
	{
		/// <summary>
		/// The type of <see cref="EditorResources"/> if it exists.
		/// </summary>
		private static readonly System.Type s_EditorResourcesType =
			typeof(EditorStylesX).Assembly.GetType("Candlelight.EditorResources");
		/// <summary>
		/// The method to load a tinted texture, if it is available.
		/// </summary>
		private static readonly MethodInfo s_EditorResourcesLoadTinted = s_EditorResourcesType == null ?
			null : s_EditorResourcesType.GetStaticMethod("LoadTinted");

		/// <summary>
		/// Gets the current builtin skin.
		/// </summary>
		/// <value>The current builtin skin.</value>
		public static GUISkin CurrentBuiltinSkin
		{
			get
			{
				return EditorGUIUtility.isProSkin ?
					EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene) :
					EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
			}
		}
		/// <summary>
		/// Gets a value indicating whether the builtin skin is in use.
		/// </summary>
		/// <value><see langword="true"/> if the builtin skin is in use; otherwise, <see langword="false"/>.</value>
		public static bool IsUsingBuiltinSkin { get { return GUI.skin == CurrentBuiltinSkin; } }

		#region Styles
		#region Backing Fields
		private static GUIStyle s_BoldFoldout = null;
		private static GUIStyle s_BoldLabel = null;
		private static GUIStyle s_BoldTitleBar = null;
		private static GUIStyle s_Box = null;
		private static GUIStyle s_BrightTab = null;
		private static GUIStyle s_EmptyInspectorArea = null;
		private static GUIStyle s_EmptySceneBox = null;
		private static GUIStyle s_EvenBackground = null;
		private static GUIStyle s_Foldout = null;
		private static GUIStyle s_Label = null;
		private static GUIStyle s_LabelRight = null;
		private static GUIStyle s_ListItem = null;
		private static GUIStyle s_MiniButton = null;
		private static GUIStyle s_MiniButtonLeft = null;
		private static GUIStyle s_MiniButtonRight = null;
		private static GUIStyle s_MiniLabel = null;
		private static GUIStyle s_DarkTab = null;
		private static GUIStyle s_OddBackground = null;
		private static GUIStyle s_OkayStatusIconStyle = null;
		private static GUIStyle s_PadlockToggle = null;
		private static GUIStyle s_PreDropGlow = null;
		private static GUIStyle s_PropertyFieldHorizontalLayoutBlock = null;
		private static GUIStyle s_PropertyFieldHorizontalLayoutBlockDefault = null;
		private static GUIStyle s_SceneBox = null;
		private static GUIStyle s_SceneGUIInspectorBackground = null;
		private static GUIStyle s_SceneNotification = null;
		private static GUIStyle s_SelectedListItem = null;
		private static GUIStyle s_StatusIconStyle = null;
		private static GUIStyle s_TabBackground = null;
		private static GUIStyle s_ThumbnailButton = null;
		#endregion

		/// <summary>
		/// Gets the bold foldout.
		/// </summary>
		/// <value>The bold foldout.</value>
		public static GUIStyle BoldFoldout
		{
			get
			{
				if (s_BoldFoldout == null)
				{
					try
					{
						s_BoldFoldout = new GUIStyle(Foldout);
						s_BoldFoldout.fontStyle = FontStyle.Bold;
					}
					catch { s_BoldFoldout = GetErrorGUIStyle(); }
				}
				return s_BoldFoldout;
			}
		}
		/// <summary>
		/// Gets the bold label.
		/// </summary>
		/// <value>The bold label.</value>
		public static GUIStyle BoldLabel
		{
			get
			{
				if (s_BoldLabel == null)
				{
					// NOTE: EditorStyles.boldLabel cannot be called in ctor, but this approach can be
					try { s_BoldLabel = GUI.skin.GetStyle("BoldLabel"); }
					catch { s_BoldLabel = GetErrorGUIStyle(); }
				}
				return s_BoldLabel;
			}
		}
		/// <summary>
		/// Gets the bold title bar.
		/// </summary>
		/// <value>The bold title bar.</value>
		public static GUIStyle BoldTitleBar
		{
			get
			{
				if (s_BoldTitleBar == null)
				{
					try { s_BoldTitleBar = GUI.skin.GetStyle("OL Title"); }
					catch { s_BoldTitleBar = GetErrorGUIStyle(); }
				}
				return s_BoldTitleBar;
			}
		}
		/// <summary>
		/// Gets the box.
		/// </summary>
		/// <value>The box.</value>
		public static GUIStyle Box
		{
			get
			{
				if (s_Box == null)
				{
					try { s_Box = new GUIStyle(GUI.skin.GetStyle("Box")); }
					catch { s_Box = GetErrorGUIStyle(); }
				}
				return s_Box;
			}
		}
		/// <summary>
		/// Gets the bright tab.
		/// </summary>
		/// <value>The bright tab.</value>
		public static GUIStyle BrightTab
		{
			get
			{
				if (s_BrightTab == null)
				{
					try
					{
						s_BrightTab = new GUIStyle(GUI.skin.GetStyle("dragtab"));
						s_BrightTab.normal.background = s_BrightTab.onNormal.background;
						if (EditorGUIUtility.isProSkin)
						{
							// create brighter texture for on normal
							Texture2D tx = s_BrightTab.onNormal.background.GetReadableCopy();
							if (tx != null)
							{
								Color[] pixels = tx.GetPixels();
								for (int i=0; i<pixels.Length; ++i)
								{
									float a = pixels[i].a;
									pixels[i] = pixels[i] * 1.2f;
									pixels[i].a = a;
								}
								tx.SetPixels(pixels);
								tx.Apply();
								s_BrightTab.onNormal.background = tx;
							}
							else
							{
								s_BrightTab.normal.textColor =
									s_BrightTab.normal.textColor * new Color(1f, 1f, 1f, 0.5f);
								s_BrightTab.onNormal.background =
									GUI.skin.GetStyle("dragtabbright").onNormal.background;
							}
						}
						else
						{
							s_BrightTab.normal.textColor = s_BrightTab.normal.textColor * new Color(1f, 1f, 1f, 0.5f);
							s_BrightTab.onNormal.background = GUI.skin.GetStyle("dragtabbright").onNormal.background;
						}
						s_BrightTab.clipping = TextClipping.Clip;
						s_BrightTab.richText = true;
						s_BrightTab.focused.background = s_BrightTab.normal.background;
						s_BrightTab.onFocused.background = s_BrightTab.onNormal.background;
						s_BrightTab.focused.textColor = EditorStyles.label.onFocused.textColor;
						s_BrightTab.onFocused.textColor = EditorStyles.label.onFocused.textColor;
					}
					catch { s_BrightTab = GetErrorGUIStyle(); }
				}
				return s_BrightTab;
			}
		}
		/// <summary>
		/// Gets the dark tab style.
		/// </summary>
		/// <value>The dark tab style.</value>
		public static GUIStyle DarkTab
		{
			get
			{
				if (s_DarkTab == null)
				{
					try
					{
						if (EditorGUIUtility.isProSkin)
						{
							s_DarkTab = new GUIStyle(GUI.skin.GetStyle("dragtab"));
							Texture2D tx = s_DarkTab.onNormal.background.GetReadableCopy();
							if (tx != null)
							{
								Color[] pixels = tx.GetPixels();
								for (int i=0; i<pixels.Length; ++i)
								{
									float a = pixels[i].a;
									pixels[i] = pixels[i] * 0.85f;
									pixels[i].a = a;
								}
								tx.SetPixels(pixels);
								tx.Apply();
								s_DarkTab.normal.background = tx;
							}
							else
							{
								s_DarkTab = new GUIStyle(BrightTab);
							}
						}
						else
						{
							s_DarkTab = new GUIStyle(BrightTab);
						}
						s_DarkTab.clipping = TextClipping.Clip;
					}
					catch { s_DarkTab = GetErrorGUIStyle(); }
				}
				return s_DarkTab;
			}
		}
		/// <summary>
		/// Gets the empty inspector area.
		/// </summary>
		/// <remarks>
		/// This style is necessary at the top level of the inspector GUI to get a PropertyField to line up.
		/// </remarks>
		/// <value>The empty inspector area.</value>
		public static GUIStyle EmptyInspectorArea
		{
			get
			{
				if (s_EmptyInspectorArea == null)
				{
					s_EmptyInspectorArea = new GUIStyle();
					s_EmptyInspectorArea.padding = Box.padding;
				}
				return s_EmptyInspectorArea;
			}
		}
		/// <summary>
		/// Gets the empty scene box.
		/// </summary>
		/// <value>The empty scene box.</value>
		public static GUIStyle EmptySceneBox
		{
			get
			{
				if (s_EmptySceneBox == null)
				{
					s_EmptySceneBox = new GUIStyle(SceneBox);
					s_EmptySceneBox.normal.background = null;
				}
				return s_EmptySceneBox;
			}
		}
		/// <summary>
		/// Gets the even background.
		/// </summary>
		/// <value>The even background.</value>
		public static GUIStyle EvenBackground
		{
			get
			{
				if (s_EvenBackground == null)
				{
					s_EvenBackground = new GUIStyle();
					s_EvenBackground.normal.background = EvenBackgroundTexture;
				}
				return s_EvenBackground;
			}
		}
		/// <summary>
		/// Gets the foldout.
		/// </summary>
		/// <value>The foldout.</value>
		public static GUIStyle Foldout
		{
			get
			{
				if (s_Foldout == null)
				{
					try { s_Foldout = GUI.skin.GetStyle("Foldout"); }
					catch { s_Foldout = GetErrorGUIStyle(); }
				}
				return s_Foldout;
			}
		}
		/// <summary>
		/// Gets the label.
		/// </summary>
		/// <value>The label.</value>
		public static GUIStyle Label
		{
			get
			{
				if (s_Label == null)
				{
					// NOTE: EditorStyles.label cannot be called in ctor, but this approach can be
					try { s_Label = GUI.skin.GetStyle("Label"); }
					catch { s_Label = GetErrorGUIStyle(); }
				}
				return s_Label;
			}
		}
		/// <summary>
		/// Gets the right-aligned label.
		/// </summary>
		/// <value>The right-aligned label.</value>
		public static GUIStyle LabelRight
		{
			get
			{
				if (s_LabelRight == null)
				{
					try
					{
						s_LabelRight = new GUIStyle(Label);
						s_LabelRight.alignment = TextAnchor.UpperRight;
					}
					catch { s_LabelRight = GetErrorGUIStyle(); }
				}
				return s_LabelRight;
			}
		}
		/// <summary>
		/// Gets the list item.
		/// </summary>
		/// <value>The list item.</value>
		public static GUIStyle ListItem
		{
			get
			{
				if (s_ListItem == null)
				{
					s_ListItem = new GUIStyle(GUI.skin.GetStyle("IN SelectedLine"));
				}
				return s_ListItem;
			}
		}
		/// <summary>
		/// Gets the mini button.
		/// </summary>
		/// <value>The mini button.</value>
		public static GUIStyle MiniButton
		{
			get
			{
				if (s_MiniButton == null)
				{
					try { s_MiniButton = GUI.skin.GetStyle("button"); }
					catch { s_MiniButton = GetErrorGUIStyle(); }
				}
				return s_MiniButton;
			}
		}
		/// <summary>
		/// Gets the left mini button.
		/// </summary>
		/// <value>The left mini button.</value>
		public static GUIStyle MiniButtonLeft
		{
			get
			{
				if (s_MiniButtonLeft == null)
				{
					try { s_MiniButtonLeft = GUI.skin.GetStyle("minibuttonleft"); }
					catch { s_MiniButtonLeft = GetErrorGUIStyle(); }
				}
				return s_MiniButtonLeft;
			}
		}
		/// <summary>
		/// Gets the right mini button.
		/// </summary>
		/// <value>The right mini button.</value>
		public static GUIStyle MiniButtonRight
		{
			get
			{
				if (s_MiniButtonRight == null)
				{
					try { s_MiniButtonRight = GUI.skin.GetStyle("minibuttonright"); }
					catch { s_MiniButtonRight = GetErrorGUIStyle(); }
				}
				return s_MiniButtonRight;
			}
		}
		/// <summary>
		/// Gets the mini label.
		/// </summary>
		/// <value>The mini label.</value>
		public static GUIStyle MiniLabel
		{
			get
			{
				if (s_MiniLabel == null)
				{
					try { s_MiniLabel = new GUIStyle(EditorStyles.miniLabel); }
					catch { s_MiniLabel = GetErrorGUIStyle(); }
				}
				return s_MiniLabel;
			}
		}
		/// <summary>
		/// Gets the odd background.
		/// </summary>
		/// <value>The odd background.</value>
		public static GUIStyle OddBackground
		{
			get
			{
				if (s_OddBackground == null)
				{
					s_OddBackground = new GUIStyle();
					s_OddBackground.normal.background = OddBackgroundTexture;
				}
				return s_OddBackground;
			}
		}
		/// <summary>
		/// Gets the status icon style.
		/// </summary>
		/// <remarks>
		/// Used to make boxes of a fixed size for status icons.
		/// </remarks>
		/// <value>The status icon style.</value>
		public static GUIStyle OkayStatusIconStyle
		{
			get
			{
				if (s_OkayStatusIconStyle == null)
				{
					s_OkayStatusIconStyle = new GUIStyle();
					s_OkayStatusIconStyle.normal.textColor = Label.normal.textColor;
					s_OkayStatusIconStyle.alignment = TextAnchor.MiddleCenter;
				}
				return s_OkayStatusIconStyle;
			}
		}
		/// <summary>
		/// Gets the padlock toggle.
		/// </summary>
		/// <value>The padlock toggle.</value>
		public static GUIStyle PadlockToggle
		{
			get
			{
				if (s_PadlockToggle == null)
				{
					s_PadlockToggle = new GUIStyle();
					string suffix = EditorGUIUtility.isProSkin ? ".png" : "";
					s_PadlockToggle.active.background = GetBuiltinTexture("IN LockButton act" + suffix);
					s_PadlockToggle.onActive.background = GetBuiltinTexture("IN LockButton on act" + suffix);
					s_PadlockToggle.normal.background = GetBuiltinTexture("IN LockButton" + suffix);
					s_PadlockToggle.onNormal.background = GetBuiltinTexture("IN LockButton on" + suffix);
					int height = s_PadlockToggle.normal.background.height;
					int width = s_PadlockToggle.normal.background.width;
					s_PadlockToggle.border = new RectOffset(width, 0, height, 0);
					s_PadlockToggle.margin = new RectOffset(4, 4, 2, 2);
					s_PadlockToggle.padding = new RectOffset(width + 3, 3, 1, 2);
					s_PadlockToggle.fixedHeight = height;
					s_PadlockToggle.fixedWidth = width;
				}
				return s_PadlockToggle;
			}
		}
		/// <summary>
		/// Gets the pre drop glow style.
		/// </summary>
		/// <value>The pre drop glow style.</value>
		public static GUIStyle PreDropGlow
		{
			get
			{
				if (s_PreDropGlow == null)
				{
					try
					{
						s_PreDropGlow = new GUIStyle(GUI.skin.GetStyle("TL SelectionButton PreDropGlow"));
						s_PreDropGlow.stretchHeight = true;
						s_PreDropGlow.stretchWidth = true;
					}
					catch { s_PreDropGlow = GetErrorGUIStyle(); }
				}
				return s_PreDropGlow;
			}
		}
		/// <summary>
		/// Gets the property field horizontal layout block style.
		/// </summary>
		/// <remarks>
		/// Use this when laying out PropertyFields inside of HorizontalLayout blocks along with other GUI in order to
		/// prevent large margins.
		/// </remarks>
		/// <value>The property field horizontal layout block style.</value>
		public static GUIStyle PropertyFieldHorizontalLayoutBlock
		{
			get
			{
				if (s_PropertyFieldHorizontalLayoutBlock == null)
				{
					s_PropertyFieldHorizontalLayoutBlock = new GUIStyle();
					s_PropertyFieldHorizontalLayoutBlock.margin = new RectOffset(0, 0, -4, -4);
					s_PropertyFieldHorizontalLayoutBlock.padding = new RectOffset();
				}
				return s_PropertyFieldHorizontalLayoutBlock;
			}
		}
		/// <summary>
		/// Gets the property field horizontal layout block default.
		/// </summary>
		/// <remarks>
		/// Use this when laying out PropertyFields inside of HorizontalLayout blocks in order to prevent large margins.
		/// </remarks>
		/// <value>The property field horizontal layout block default.</value>
		public static GUIStyle PropertyFieldHorizontalLayoutBlockDefault
		{
			get
			{
				if (s_PropertyFieldHorizontalLayoutBlockDefault == null)
				{
					s_PropertyFieldHorizontalLayoutBlockDefault = new GUIStyle();
					s_PropertyFieldHorizontalLayoutBlockDefault.margin = new RectOffset(0, 0, -2, -2);
				}
				return s_PropertyFieldHorizontalLayoutBlockDefault;
			}
		}
		/// <summary>
		/// Gets the scene box.
		/// </summary>
		/// <value>The scene box.</value>
		public static GUIStyle SceneBox
		{
			get
			{
				if (s_SceneBox == null)
				{
					try { s_SceneBox = new GUIStyle(GUI.skin.GetStyle("sv_iconselector_back")); }
					catch { s_SceneBox = GetErrorGUIStyle(); }
					s_SceneBox.padding = new RectOffset(4, 4, 4, 4);
					s_SceneBox.stretchHeight = false;
				}
				return s_SceneBox;
			}
		}
		/// <summary>
		/// Gets the scene GUI inspector background.
		/// </summary>
		/// <value>The scene GUI inspector background.</value>
		public static GUIStyle SceneGUIInspectorBackground
		{
			get
			{
				if (s_SceneGUIInspectorBackground == null)
				{
					try
					{
						s_SceneGUIInspectorBackground = new GUIStyle(GUI.skin.GetStyle("Box"));
						s_SceneGUIInspectorBackground.margin.left = 1;
						s_SceneGUIInspectorBackground.margin.right = 0;
					}
					catch { s_SceneGUIInspectorBackground = GetErrorGUIStyle(); }
				}
				return s_SceneGUIInspectorBackground;
			}
		}
		/// <summary>
		/// Gets the scene notification style.
		/// </summary>
		/// <value>The scene notification style.</value>
		public static GUIStyle SceneNotification
		{
			get
			{
				if (s_SceneNotification == null)
				{
					s_SceneNotification = new GUIStyle(GUI.skin.GetStyle("NotificationBackground"));
				}
				return s_SceneNotification;
			}
		}
		/// <summary>
		/// Gets the selected list item.
		/// </summary>
		/// <value>The selected list item.</value>
		public static GUIStyle SelectedListItem
		{
			get
			{
				if (s_SelectedListItem == null)
				{
					s_SelectedListItem = new GUIStyle(GUI.skin.GetStyle("IN SelectedLine"));
					s_SelectedListItem.normal.background = s_SelectedListItem.onNormal.background;
				}
				return s_SelectedListItem;
			}
		}
		/// <summary>
		/// Gets the status icon style.
		/// </summary>
		/// <remarks>
		/// Used to make boxes of a fixed size for status icons.
		/// </remarks>
		/// <value>The status icon style.</value>
		public static GUIStyle StatusIconStyle
		{
			get
			{
				if (s_StatusIconStyle == null)
				{
					s_StatusIconStyle = new GUIStyle();
					s_StatusIconStyle.normal.textColor = Label.normal.textColor;
					s_StatusIconStyle.alignment = TextAnchor.MiddleCenter;
					// account for empty space baked into icons
					s_StatusIconStyle.fixedHeight = 22f;
					s_StatusIconStyle.fixedWidth = 22f;
					s_StatusIconStyle.contentOffset = -2f * Vector2.one;
				}
				return s_StatusIconStyle;
			}
		}
		/// <summary>
		/// Gets the tab background.
		/// </summary>
		/// <value>The tab background.</value>
		public static GUIStyle TabAreaBackground
		{
			get
			{
				if (s_TabBackground == null)
				{
					try
					{
						s_TabBackground = new GUIStyle(GUI.skin.GetStyle("Box"));
						s_TabBackground.margin.top = 0;
					}
					catch { s_TabBackground = GetErrorGUIStyle(); }
				}
				return s_TabBackground;
			}
		}
		/// <summary>
		/// Gets the thumbnail button.
		/// </summary>
		/// <value>The thumbnail button.</value>
		public static GUIStyle ThumbnailButton
		{
			get
			{
				if (s_ThumbnailButton == null)
				{
					s_ThumbnailButton = new GUIStyle(EditorStylesX.MiniButton);
					s_ThumbnailButton.padding = new RectOffset(2, 2, 2, 2);
				}
				return s_ThumbnailButton;
			}
		}
		
		/// <summary>
		/// Gets the error GUI style.
		/// </summary>
		/// <remarks>
		/// Used when loading a built-in style fails
		/// </remarks>
		/// <returns>The error GUI style.</returns>
		private static GUIStyle GetErrorGUIStyle()
		{
			GUIStyle s = new GUIStyle();
			s.normal.textColor = Color.magenta;
			return s;
		}
		#endregion

		#region Textures
		#region Backing Fields
		private static Texture2D s_CopyIcon;
		private static Texture2D s_CopySymmetricalIcon;
		private static Texture2D s_ErrorIcon;
		private static Texture2D s_EvenBackgroundTexture;
		private static Texture2D s_InfoIcon;
		private static Texture2D s_LockedIcon;
		private static Texture2D s_OddBackgroundTexture;
		private static Texture2D s_OkayIcon;
		private static Texture2D s_PasteAllIcon;
		private static Texture2D s_PasteIcon;
		private static Texture2D s_PasteSymmetricalIcon;
		private static Texture2D s_ResetIcon;
		private static Texture2D s_UnlockedIcon;
		private static Texture2D s_WarningIcon;
		#endregion

		/// <summary>
		/// Gets the copy icon.
		/// </summary>
		/// <value>The copy icon.</value>
		public static Texture2D CopyIcon
		{
			get
			{
				return s_CopyIcon = s_CopyIcon ?? (
					s_EditorResourcesLoadTinted == null ?
						null :
						(Texture2D)s_EditorResourcesLoadTinted.Invoke(null, new [] { "Icon - Copy.psd" })
				);
			}
		}
		/// <summary>
		/// Gets the copy symmetrical icon.
		/// </summary>
		/// <value>The copy symmetrical icon.</value>
		public static Texture2D CopySymmetricalIcon
		{
			get
			{
				return s_CopySymmetricalIcon = s_CopySymmetricalIcon ?? (
					s_EditorResourcesLoadTinted == null ?
						null :
						(Texture2D)s_EditorResourcesLoadTinted.Invoke(null, new [] { "Icon - Copy Symmetrical.psd" })
				);

			}
		}
		/// <summary>
		/// Gets the error icon.
		/// </summary>
		/// <value>The error icon.</value>
		public static Texture2D ErrorIcon
		{
			get { return s_ErrorIcon = s_ErrorIcon ?? EditorGUIUtility.FindTexture("console.erroricon"); }
		}
		/// <summary>
		/// Gets the even background texture.
		/// </summary>
		/// <value>The even background texture.</value>
		public static Texture2D EvenBackgroundTexture
		{
			get
			{
				if (s_EvenBackgroundTexture == null)
				{
					try { s_EvenBackgroundTexture = GUI.skin.GetStyle("CN EntryBackEven").normal.background; }
					catch { s_EvenBackgroundTexture = EditorGUIUtility.whiteTexture; }
				}
				return s_EvenBackgroundTexture;
			}
		}
		/// <summary>
		/// Gets the info icon.
		/// </summary>
		/// <value>The info icon.</value>
		public static Texture2D InfoIcon
		{
			get { return s_InfoIcon = s_InfoIcon ?? EditorGUIUtility.FindTexture("console.infoicon"); }
		}
		/// <summary>
		/// Gets the locked icon.
		/// </summary>
		/// <value>The locked icon.</value>
		public static Texture2D LockedIcon
		{
			get
			{
				if (s_LockedIcon == null)
				{
					s_LockedIcon = GetBuiltinTexture("IN LockButton on") ??
						GUI.skin.GetStyle("IN LockButton").onNormal.background ??
						EditorGUIUtility.whiteTexture;
				}
				return s_LockedIcon;
			}
		}
		/// <summary>
		/// Gets the odd background texture.
		/// </summary>
		/// <value>The odd background texture.</value>
		public static Texture2D OddBackgroundTexture
		{
			get
			{
				if (s_OddBackgroundTexture == null)
				{
					try { s_OddBackgroundTexture = GUI.skin.GetStyle("CN EntryBackOdd").normal.background; }
					catch { s_OddBackgroundTexture = EditorGUIUtility.whiteTexture; }
				}
				return s_OddBackgroundTexture;
			}
		}
		/// <summary>
		/// Gets the okay icon.
		/// </summary>
		/// <value>The okay icon.</value>
		public static Texture2D OkayIcon
		{
			get
			{
				if (s_OkayIcon == null)
				{
					try { s_OkayIcon = GUI.skin.GetStyle("MenuItem").onNormal.background; }
					catch { s_OkayIcon = EditorGUIUtility.whiteTexture; }
				}
				return s_OkayIcon;
			}
		}
		/// <summary>
		/// Gets the paste all icon.
		/// </summary>
		/// <value>The paste all icon.</value>
		public static Texture2D PasteAllIcon
		{
			get
			{
				return s_PasteAllIcon = s_PasteAllIcon ?? (
					s_EditorResourcesLoadTinted == null ?
					null :
					(Texture2D)s_EditorResourcesLoadTinted.Invoke(null, new [] { "Icon - Paste All.psd" })
				);
			}
		}
		/// <summary>
		/// Gets the paste icon.
		/// </summary>
		/// <value>The paste icon.</value>
		public static Texture2D PasteIcon
		{
			get
			{
				return s_PasteIcon = s_PasteIcon ?? (
					s_EditorResourcesLoadTinted == null ?
						null :
						(Texture2D)s_EditorResourcesLoadTinted.Invoke(null, new [] { "Icon - Paste.psd" })
				);
			}
		}
		/// <summary>
		/// Gets the paste symmetrical icon.
		/// </summary>
		/// <value>The paste symmetrical icon.</value>
		public static Texture2D PasteSymmetricalIcon
		{
			get
			{
				return s_PasteSymmetricalIcon = s_PasteSymmetricalIcon ?? (
					s_EditorResourcesLoadTinted == null ?
						null :
						(Texture2D)s_EditorResourcesLoadTinted.Invoke(null, new [] { "Icon - Paste Symmetrical.psd" })
				);
			}
		}
		/// <summary>
		/// Gets the reset icon.
		/// </summary>
		/// <value>The reset icon.</value>
		public static Texture2D ResetIcon
		{
			get
			{
				return s_ResetIcon = s_ResetIcon ?? (
					s_EditorResourcesLoadTinted == null ?
					null :
					(Texture2D)s_EditorResourcesLoadTinted.Invoke(null, new [] { "Icon - Reset.psd" })
				);
			}
		}
		/// <summary>
		/// Gets the unlocked icon.
		/// </summary>
		/// <remarks>
		/// Light version.
		/// </remarks>
		/// <value>The unlocked icon.</value>
		public static Texture2D UnlockedIcon
		{
			get
			{
				if (s_UnlockedIcon == null)
				{
					s_UnlockedIcon = GetBuiltinTexture("IN LockButton") ??
						GUI.skin.GetStyle("IN LockButton").normal.background ??
						EditorGUIUtility.whiteTexture; 
				}
				return s_UnlockedIcon;
			}
		}
		/// <summary>
		/// Gets the warning icon.
		/// </summary>
		/// <value>The warning icon.</value>
		public static Texture2D WarningIcon
		{
			get { return s_WarningIcon = s_WarningIcon ?? EditorGUIUtility.FindTexture("console.warnicon"); }
		}

		/// <summary>
		/// Gets the builtin texture with the specified name.
		/// </summary>
		/// <returns>The builtin texture with the specified name.</returns>
		/// <param name="textureName">Texture name.</param>
		public static Texture2D GetBuiltinTexture(string textureName)
		{
			Texture2D[] result = GetBuiltinTextures(textureName);
			return result == null || result.Length == 0 ? null : result[0];
		}

		/// <summary>
		/// Gets the builtin textures.
		/// </summary>
		/// <returns>The builtin textures with the specified name.</returns>
		/// <param name="textureName">Texture name.</param>
		public static Texture2D[] GetBuiltinTextures(string textureName)
		{
			string withExt = textureName + ".png";
			return new List<Texture2D>(
				Resources.FindObjectsOfTypeAll(typeof(Texture2D)) as Texture2D[]
			).FindAll(item => item.name == textureName || item.name == withExt).ToArray();
		}
		#endregion
	}
}