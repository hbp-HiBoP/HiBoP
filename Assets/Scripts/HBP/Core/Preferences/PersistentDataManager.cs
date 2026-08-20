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

        public static FilterPresetRecoveryReport PendingFilterRecoveryReport { get; private set; } = FilterPresetRecoveryReport.Empty;
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
                    PendingFilterRecoveryReport = FilterPresetRecoveryService.Recover(m_Tags, m_FilterConditionsPresets, TagParsingPolicy.Default);
                    new LoadingContext(m_Tags.AllTags, Array.Empty<Protocol>(), logLegacyEnumWarnings: true).ResolveFilterConditions(m_FilterConditionsPresets);
                    if (m_Tags.HasUnsavedTagMigration) m_Tags.SaveRecovered();
                    if (PendingFilterRecoveryReport.HasChanges) m_FilterConditionsPresets.SaveRecovered();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public static FilterPresetRecoveryReport ConsumeFilterRecoveryReport()
        {
            FilterPresetRecoveryReport report = PendingFilterRecoveryReport;
            PendingFilterRecoveryReport = FilterPresetRecoveryReport.Empty;
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
