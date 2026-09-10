using HBP.Core.Data;
using HBP.Core.Tools;
using System;
using UnityEngine;

namespace HBP.Core.Preferences
{
    public class PersistentDataManager : Manager<PersistentDataManager>
    {
        #region Properties

        private UserPreferences m_UserPreferences;

        public static UserPreferences UserPreferences
        {
            get { return m_Instance.m_UserPreferences; }
        }

        private TagCollection m_Tags;

        public static TagCollection Tags
        {
            get { return m_Instance.m_Tags; }
        }

        public static Exception TagInitializationException { get; private set; }

        public static FilterPresetRepairReport PendingFilterRepairReport { get; private set; } = FilterPresetRepairReport.Empty;
        public static Exception FilterInitializationException { get; private set; }
        private static bool s_FilterInitializationWarningPresented;

        private AliasCollection m_Aliases;

        public static AliasCollection Aliases
        {
            get { return m_Instance.m_Aliases; }
        }

        private FilterConditionsPresetCollection m_FilterConditionsPresets;

        public static FilterConditionsPresetCollection FilterConditionsPresets
        {
            get { return m_Instance.m_FilterConditionsPresets; }
        }

        #endregion

        #region Private Methods

        /// <summary>Install a received session context in memory after all previous scenes have closed.</summary>
        public static void ApplySessionData(UserPreferences preferences, TagCollection tags, AliasCollection aliases, FilterConditionsPresetCollection filterPresets)
        {
            if (!IsInitialized) throw new InvalidOperationException("Persistent data must be initialized before installing session data.");
            if (preferences == null || tags == null || aliases == null || filterPresets == null) throw new ArgumentNullException("Session data is incomplete.");
            m_Instance.m_UserPreferences = preferences;
            m_Instance.m_Tags = tags;
            m_Instance.m_Aliases = aliases;
            m_Instance.m_FilterConditionsPresets = filterPresets;
        }

        protected override void Initialization()
        {
            base.Initialization();
            m_UserPreferences = UserPreferences.Initialize();
            m_Tags = TagCollection.Initialize(out Exception tagInitializationException);
            TagInitializationException = tagInitializationException;
            m_Aliases = AliasCollection.Initialize();
            m_FilterConditionsPresets = FilterConditionsPresetCollection.Initialize(out Exception filterInitializationException);
            FilterInitializationException = filterInitializationException;
            s_FilterInitializationWarningPresented = false;
            if (TagInitializationException == null && FilterInitializationException == null)
            {
                try
                {
                    PendingFilterRepairReport = FilterPresetRepairService.Repair(m_Tags, m_FilterConditionsPresets, TagParsingPolicy.Default);
                    new LoadingContext(m_Tags.AllTags, Array.Empty<Protocol>(), logLegacyEnumWarnings: true).ResolveFilterConditions(m_FilterConditionsPresets);
                    if (m_Tags.HasUnsavedTagMigration) m_Tags.Save();
                    if (PendingFilterRepairReport.HasChanges) m_FilterConditionsPresets.Save();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public static FilterPresetRepairReport ConsumeFilterRepairReport()
        {
            FilterPresetRepairReport report = PendingFilterRepairReport;
            PendingFilterRepairReport = FilterPresetRepairReport.Empty;
            return report;
        }

        public static Exception ConsumeFilterInitializationWarning()
        {
            if (s_FilterInitializationWarningPresented) return null;
            s_FilterInitializationWarningPresented = true;
            return FilterInitializationException;
        }

        #endregion
    }
}
