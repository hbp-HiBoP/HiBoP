// 
// EditorPreferenceMenu.cs
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

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Candlelight
{
	/// <summary>
	/// An enum for different Candlelight Interactive products.
	/// </summary>
	public enum AssetStoreProduct
	{
		None = -1,
		RagdollWorkshop = 39,
		CustomHandles = 161,
		PropertyBackingField = 18253,
		HyperText = 21252,
		Other
	}

	/// <summary>
	/// A class for the Candlelight editor preferences menu.
	/// </summary>
	public class EditorPreferenceMenu : Singleton<EditorPreferenceMenu>
	{
#pragma warning disable 414
		/// <summary>
		/// The asset store window.
		/// </summary>
		private static EditorWindow s_AssetStoreWindow = null;
		/// <summary>
		/// The label for the auto symbol toggle.
		/// </summary>
		private static readonly GUIContent s_AutoSymbolLabel = new GUIContent(
			"Auto Detect Products",
			"Disable only if you do not want scripting define symbols automatically added for each product."
		);
		/// <summary>
		/// The type of the asset store window.
		/// </summary>
		private static readonly System.Type s_AssetStoreWindowType =
			typeof(EditorWindow).Assembly.GetTypes().FirstOrDefault(t => t.Name == "AssetStoreWindow");
#pragma warning restore 414
		/// <summary>
		/// The bug report email address.
		/// </summary>
		private static readonly string s_BugReportEmailAddress = "bugs@candlelightinteractive.com";
		/// <summary>
		/// The method to display the scene gui toggle, if it is available.
		/// </summary>
		private static readonly System.Reflection.MethodInfo s_DisplaySceneGUIToggle =
			typeof(EditorGUIX).GetStaticMethod("DisplaySceneGUIToggle");
		/// <summary>
		/// The publisher page URL in the asset store.
		/// </summary>
		private static readonly string s_PublisherPage = "com.unity3d.kharma:publisher/8";
		/// <summary>
		/// The product forum URL format string.
		/// </summary>
		private static readonly string s_ProductForumUrlFormat =
			"https://groups.google.com/a/candlelightinteractive.com/forum/#!categories/developer-support/{0}";
		/// <summary>
		/// The product forum URL for each feature.
		/// </summary>
		private static readonly Dictionary<AssetStoreProduct, string> s_ProductForumUrls =
			new Dictionary<AssetStoreProduct, string>()
		{
			{ AssetStoreProduct.CustomHandles, string.Format(s_ProductForumUrlFormat, "uas-custom-handles") },
			{ AssetStoreProduct.HyperText, string.Format(s_ProductForumUrlFormat, "uas-hypertext") },
			{ AssetStoreProduct.PropertyBackingField, string.Format(s_ProductForumUrlFormat, "free-and-open-source") },
			{ AssetStoreProduct.RagdollWorkshop, string.Format(s_ProductForumUrlFormat, "uas-ragdoll") }
		};
		/// <summary>
		/// The menu items for each product.
		/// </summary>
		private static readonly Dictionary<AssetStoreProduct, List<MethodInfo>> s_ProductMenuItems =
			new Dictionary<AssetStoreProduct, List<MethodInfo>>();
		/// <summary>
		/// The product page URL format string
		/// </summary>
		private static readonly string s_ProductPageUrlFormat = "com.unity3d.kharma:content/{0}";
		/// <summary>
		/// The asset store URL for each product.
		/// </summary>
		private static readonly Dictionary<AssetStoreProduct, string> s_ProductPageUrls =
			new Dictionary<AssetStoreProduct, string>()
		{
			{
				AssetStoreProduct.RagdollWorkshop,
				string.Format(s_ProductPageUrlFormat, (int)AssetStoreProduct.RagdollWorkshop)
			},
			{
				AssetStoreProduct.CustomHandles,
				string.Format(s_ProductPageUrlFormat, (int)AssetStoreProduct.CustomHandles)
			},
			{
				AssetStoreProduct.PropertyBackingField,
				string.Format(s_ProductPageUrlFormat, (int)AssetStoreProduct.PropertyBackingField)
			},
			{
				AssetStoreProduct.HyperText,
				string.Format(s_ProductPageUrlFormat, (int)AssetStoreProduct.HyperText)
			}
		};
		/// <summary>
		/// The tab indices for each asset store product.
		/// </summary>
		private static readonly Dictionary<AssetStoreProduct, int> s_ProductTabIndices =
			new Dictionary<AssetStoreProduct, int>();
		/// <summary>
		/// The labels for the product tabs.
		/// </summary>
		private static GUIContent[] s_ProductTabLabels = new GUIContent[0];
		/// <summary>
		/// The tab pages.
		/// </summary>
		[System.NonSerialized]
		private static readonly Dictionary<int, System.Action> s_TabPages = new Dictionary<int, System.Action>();

		#region Backing Fields
		private static GUIStyle m_TabAreaStyle = null;
		#endregion

		/// <summary>
		/// Gets the tab area style.
		/// </summary>
		/// <value>The tab area style.</value>
		private static GUIStyle TabAreaStyle
		{
			get
			{
				if (m_TabAreaStyle == null)
				{
					m_TabAreaStyle = new GUIStyle();
					m_TabAreaStyle.padding = new RectOffset(3, 3, 0, 0); // otherwise tabs spill over edges of box
				}
				return m_TabAreaStyle;
			}
		}

		/// <summary>
		/// The current tab.
		/// </summary>
		[SerializeField]
		private AssetStoreProduct m_CurrentTab = AssetStoreProduct.None;
		/// <summary>
		/// The scroll position.
		/// </summary>
		[SerializeField]
		private Vector2 m_ScrollPosition;

		/// <summary>
		/// Adds the preference menu item.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="method">Method.</param>
		public static void AddPreferenceMenuItem(AssetStoreProduct product, System.Action method)
		{
			AddPreferenceMenuItem(product, method.Method);
		}

		/// <summary>
		/// Adds the preference menu item.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="method">Method.</param>
		public static void AddPreferenceMenuItem(AssetStoreProduct product, MethodInfo method)
		{
			if (!s_ProductMenuItems.ContainsKey(product))
			{
				s_ProductMenuItems.Add(product, new List<MethodInfo>());
			}
			s_ProductTabIndices.Clear();
			int productIndex = 0;
			foreach (AssetStoreProduct p in s_ProductMenuItems.Keys)
			{
				if (p == AssetStoreProduct.None)
				{
					continue;
				}
				s_ProductTabIndices[p] = productIndex;
				++productIndex;
				if (p == AssetStoreProduct.Other)
				{
					continue;
				}
			}
			s_ProductMenuItems[product].Add(method);
			s_ProductMenuItems[product].Sort(
				(a, b) => string.Format("{0}.{1}", a.ReflectedType, a.Name).CompareTo(
					string.Format("{0}.{1}", b.ReflectedType, b.Name)
				)
			);
			List<GUIContent> labels = new List<GUIContent>();
			labels.AddRange(from tabName in s_ProductMenuItems.Keys select new GUIContent(tabName.ToString().ToWords()));
			s_ProductTabLabels = labels.ToArray();
			s_TabPages.Clear();
			for (int i = 0; i < s_ProductTabLabels.Length; ++i)
			{
				s_TabPages.Add(i, () => Instance.DisplayPreferences(Instance.m_CurrentTab));
			}
		}

		/// <summary>
		/// Displays the preference GUI.
		/// </summary>
		[PreferenceItem("Candlelight")]
		public static void DisplayPreferenceGUI()
		{
			#if !UNITY_5_6_OR_NEWER
			GUILayout.BeginArea(new Rect(134f, 39f, 352f, 352f)); // the rect in the preference window is bizarre...
			#endif
			{
				if (s_DisplaySceneGUIToggle != null)
				{
					s_DisplaySceneGUIToggle.Invoke(null, null);
				}
				UnityFeatureDefineSymbols.ShouldAutoRegisterProductSymbols = EditorGUIX.DisplayOnOffToggle(
					s_AutoSymbolLabel, UnityFeatureDefineSymbols.ShouldAutoRegisterProductSymbols
				);
				EditorGUILayout.BeginVertical(TabAreaStyle, GUILayout.ExpandWidth(false));
				{
					if (s_ProductTabIndices.Count > 0)
					{
						int tab = 0;
						if (!s_ProductTabIndices.ContainsKey(Instance.m_CurrentTab))
						{
							Instance.m_CurrentTab = s_ProductTabIndices.FirstOrDefault().Key;
						}
						EditorGUI.BeginChangeCheck();
						{
							tab = EditorGUIX.DisplayTabGroup(
								s_ProductTabIndices[Instance.m_CurrentTab], s_ProductTabLabels, s_TabPages, 4
							);
						}
						if (EditorGUI.EndChangeCheck())
						{
							Instance.m_CurrentTab = s_ProductTabIndices.Where(kv => kv.Value == tab).FirstOrDefault().Key;
						}
					}
				}
				EditorGUILayout.EndVertical();
			}
			#if !UNITY_5_6_OR_NEWER
			GUILayout.EndArea();
			#endif
		}
		
		/// <summary>
		/// Displays the bug report button.
		/// </summary>
		/// <param name="product">Product.</param>
		private static void DisplayBugReportButton(AssetStoreProduct product)
		{
			if (EditorGUIX.DisplayButton(string.Format("Report a Problem with {0}", product.ToString().ToWords())))
			{
				OpenUrl(
					string.Format(
						"mailto:{0}?subject={1} Bug Report&body=1) What happened?\n\n2) How often does it happen?\n\n" +
						"3) How can I reproduce it using the example you attached?",
						s_BugReportEmailAddress, product.ToString().ToWords()
					),
					"Error Creating Bug Report",
					"Please ensure an application is associated with email links."
				);
			}
		}
		
		/// <summary>
		/// Displays the preferences for a feature.
		/// </summary>
		/// <param name="product">Product.</param>
		private void DisplayPreferences(AssetStoreProduct product)
		{
			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
			{
				foreach (MethodInfo method in s_ProductMenuItems[product])
				{
					EditorGUILayout.LabelField(
						method.ReflectedType.IsGenericType ?
							string.Format(
								"{0} ({1})",
								method.ReflectedType.Name.ToWords().Range(0, -2),
								", ".Join(from t in method.ReflectedType.GetGenericArguments() select t.Name.ToWords())
							) : method.ReflectedType.Name.ToWords(),
						EditorStyles.boldLabel
					);
					EditorGUIX.DisplayHorizontalLine();
					EditorGUI.indentLevel += 1;
					method.Invoke(null, null);
					EditorGUI.indentLevel -= 1;
				}
			}
			EditorGUILayout.EndScrollView();
			// bug report button
			DisplayBugReportButton(product);
			// forum link button
			if (
				s_ProductForumUrls.ContainsKey(product) &&
				!string.IsNullOrEmpty(s_ProductForumUrls[product]) &&
				EditorGUIX.DisplayButton(string.Format("Get Help with {0}", product.ToString().ToWords()))
			)
			{
				OpenUrl(s_ProductForumUrls[product]);
			}
			// asset store page
			if (
				s_ProductPageUrls.ContainsKey(product) &&
				!string.IsNullOrEmpty(s_ProductPageUrls[product]) &&
				EditorGUIX.DisplayButton(
					string.Format("Review {0} on the Unity Asset Store", product.ToString().ToWords())
				)
			)
			{
				OpenUrl(s_ProductPageUrls[product]);
			}
			// products page
			if (EditorGUIX.DisplayButton("More Products by Candlelight Interactive"))
			{
				OpenUrl(s_PublisherPage);
			}
		}

		/// <summary>
		/// Opens the URL.
		/// </summary>
		/// <param name="url">URL.</param>
		/// <param name="errorDialogTitle">Error dialog title.</param>
		/// <param name="errorDialogMessage">Error dialog message.</param>
		private static void OpenUrl(
			string url,
			string errorDialogTitle = "Error Opening URL",
			string errorDialogMessage = "Please ensure an application is associated with web links."
		)
		{
			try
			{
				if (url.StartsWith("mailto:"))
				{
					System.Diagnostics.Process.Start(url);
				}
				else
				{
					Application.OpenURL(url);
				}
			}
			catch
			{
				EditorUtility.DisplayDialog(errorDialogTitle, errorDialogMessage, "OK");
			}
		}
	}
}