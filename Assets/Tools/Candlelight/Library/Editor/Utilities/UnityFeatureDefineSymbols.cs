// 
// UnityFeatureDefineSymbols.cs
// 
// Copyright (c) 2013-2017, Candlelight Interactive, LLC
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
using System.Collections.Generic;
using System.Linq;

namespace Candlelight
{
	/// <summary>
	/// A class to register define symbols for Unity features.
	/// </summary>
	[InitializeOnLoad]
	public sealed class UnityFeatureDefineSymbols
	{
		/// <summary>
		/// Initializes the <see cref="UnityFeatureDefineSymbols"/> class.
		/// </summary>
		static UnityFeatureDefineSymbols()
		{
			foreach (KeyValuePair<string, string[]> featureClasses in s_FeatureAvailabilityClasses)
			{
				SetSymbolForAllBuildTargets(
					featureClasses.Key,
					target => featureClasses.Value.Any(
						fullName => ReflectionX.AllTypes.FirstOrDefault(type => type.FullName == fullName) != null
					)
				);
			}
			UpdateProductSymbols();
		}

		#region Preferences
		private static readonly EditorPreference<bool, UnityFeatureDefineSymbols> s_AutoProductRegistrationPreference =
			EditorPreference<bool, UnityFeatureDefineSymbols>.ForToggle("autoRegistration", true);
		#endregion

		/// <summary>
		/// For each feature availability symbol, an array of class names whose presence indicates the feature.
		/// </summary>
		private static readonly Dictionary<string, string[]> s_FeatureAvailabilityClasses =
			new Dictionary<string, string[]>()
		{
			{ "IS_UNITYEDITOR_ANIMATIONS_AVAILABLE", new [] { "UnityEditor.Animations.AnimatorController" } }
		};
		/// <summary>
		/// For each product availability symbol, an array of class names whose presence indicates the feature.
		/// </summary>
		private static readonly Dictionary<string, string[]> s_ProductAvailabilityClasses =
			new Dictionary<string, string[]>()
		{
			{
				"IS_CANDLELIGHT_CUSTOM_HANDLES_AVAILABLE",
				new [] { "Candlelight.FalloffHandles", "Candlelight.HelixHandles" }
			},
			{ "IS_CANDLELIGHT_HYPERTEXT_AVAILABLE", new [] { "Candlelight.UI.HyperText" } },
			{ "IS_CANDLELIGHT_RAGDOLL_AVAILABLE", new [] { "Candlelight.Physics.RagdollAnimator", } },
			{ "IS_ROOTMOTION_FINAL_IK_AVAILABLE", new [] { "RootMotion.FinalIK.IKSolver" } }
		};

		/// <summary>
		/// Gets or sets a value indicating whether scripting define symbols should be automatically registered for
		/// products located in the project.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if scripting define symbols should be automatically registered; otherwise,
		/// <see langword="false"/> .
		/// </value>
		public static bool ShouldAutoRegisterProductSymbols
		{
			get { return s_AutoProductRegistrationPreference.CurrentValue; }
			set
			{
				if (s_AutoProductRegistrationPreference.CurrentValue != value)
				{
					s_AutoProductRegistrationPreference.CurrentValue = value;
					UpdateProductSymbols();
				}
			}
		}

		/// <summary>
		/// Sets the symbol for all build targets.
		/// </summary>
		/// <param name="symbol">Symbol.</param>
		/// <param name="condition">
		/// The condition under which the symbol should be added. If <see langword="null"/>, then the symbol will be
		/// added for all build targets. Otherwise, if the condition evaluates to <see langword="true"/> for the
		/// particular target, it will be added; if the condition evaluates to <see langword="false"/> for the
		/// particular target, it will be removed.
		/// </param>
		public static void SetSymbolForAllBuildTargets(string symbol, System.Predicate<BuildTargetGroup> condition)
		{
			foreach (BuildTargetGroup target in System.Enum.GetValues(typeof(BuildTargetGroup)))
			{
				// prevent editor spam in Unity 5.x
				if (target == BuildTargetGroup.Unknown)
				{
					continue;
				}
#if UNITY_5_3_0
				// prevent editor spam in 5.3.0
				if ((int)target == 25) // tvOS throwing out error
				{
					continue;
				}
#endif
#if UNITY_5_6_0
				// prevent editor spam in 5.6.0 beta
				if ((int)target == 27) // Switch throwing out error
				{
					continue;
				}
#endif
				using (var attrs = new ListPool<System.ObsoleteAttribute>.Scope())
				{
					System.Reflection.MemberInfo member = typeof(BuildTargetGroup).GetMember(target.ToString())[0];
					if (member.GetCustomAttributes(attrs.List) > 0)
					{
						continue;
					}
				}
				HashSet<string> symbols =
					new HashSet<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Split(';'));
				if (condition == null || condition(target))
				{
					symbols.Add(symbol);
				}
				else if (symbols.Contains(symbol))
				{
					symbols.Remove(symbol);
				}
				PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", symbols.ToArray()));
			}
		}

		/// <summary>
		/// Updates the product availability symbols.
		/// </summary>
		private static void UpdateProductSymbols()
		{
			bool autoRegister = s_AutoProductRegistrationPreference.CurrentValue;
			foreach (KeyValuePair<string, string[]> productClasses in s_ProductAvailabilityClasses)
			{
				SetSymbolForAllBuildTargets(
					productClasses.Key,
					target => autoRegister && productClasses.Value.Any(
						fullName => ReflectionX.AllTypes.FirstOrDefault(type => type.FullName == fullName) != null
					)
				);
			}
		}
	}
}