using UnityEngine;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify the user preferences.
    /// </summary>
    public class UserPreferencesModifier : ObjectModifier<UserPreferences>
    {
        #region Properties
        [SerializeField] ProjectPreferencesSubModifier m_ProjectPreferencesSubModifier;
        [SerializeField] ThemePreferencesSubModifier m_ThemePreferencesSubModifier;
        [SerializeField] LocationPreferencesSubModifier m_LocationPreferencesSubModifier;
        [SerializeField] SystemPreferencesSubModifier m_SystemPreferencesSubModifier;
        [SerializeField] MiscPreferencesSubModifier m_MiscPreferencesSubModifier;
        [SerializeField] EEGPreferencesSubModifier m_EEGPreferencesSubModifier;
        [SerializeField] ProtocolPreferencesSubModifier m_ProtocolPreferencesSubModifier;
        [SerializeField] AnatomyPreferencesSubModifier m_AnatomyPreferencesModifier;
        [SerializeField] AtlasesPreferencesSubModifier m_AtlasesPreferencesSubModifier;
        [SerializeField] _3DPreferencesSubModifier m_3DPreferencesSubModifier;
        [SerializeField] TrialMatrixPreferencesSubModifier m_TrialMatrixPreferencesSubModifier;
        [SerializeField] GraphPreferencesSubModifier m_GraphPreferencesSubModifier;
        [SerializeField] CutPreferencesSubModifier m_CutPreferencesSubModifier;
        private NormalizationType m_InitialNormalization;
        private int m_InitialMemoryCacheLimit;

        public override UserPreferences Object
        {
            get => base.Object;
            set
            {
                m_InitialNormalization = value.Data.EEG.Normalization;
                m_InitialMemoryCacheLimit = value.General.System.MemoryCacheLimit;
                base.Object = value;
            }
        }

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_ProjectPreferencesSubModifier.Interactable = value;
                m_ThemePreferencesSubModifier.Interactable = value;
                m_LocationPreferencesSubModifier.Interactable = value;
                m_SystemPreferencesSubModifier.Interactable = value;
                m_MiscPreferencesSubModifier.Interactable = value;
                m_EEGPreferencesSubModifier.Interactable = value;
                m_ProtocolPreferencesSubModifier.Interactable = value;
                m_AnatomyPreferencesModifier.Interactable = value;
                m_AtlasesPreferencesSubModifier.Interactable = value;
                m_3DPreferencesSubModifier.Interactable = value;
                m_TrialMatrixPreferencesSubModifier.Interactable = value;
                m_GraphPreferencesSubModifier.Interactable = value;
                m_CutPreferencesSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override async void OK()
        {
            NormalizationType requestedNormalization = m_ObjectTemp.Data.EEG.Normalization;
            bool normalizationChanged = requestedNormalization != m_InitialNormalization;
            bool memoryLimitChanged = m_ObjectTemp.General.System.MemoryCacheLimit != m_InitialMemoryCacheLimit;

            if (normalizationChanged && Module3DMain.Scenes.Count > 0)
            {
                int result = await DialogBoxManager.OpenAsync(
                    DialogBoxType.Warning,
                    "Reload required",
                    "The default EEG normalization has changed. Open visualizations must be reloaded to apply it.\n\n"
                    + "The cached raw recordings will be kept when possible. Would you like to save and reload now?",
                    "Save & Reload",
                    "Cancel");
                if (result != 0)
                {
                    DataManager.DefaultNormalization = m_InitialNormalization;
                    Refresh();
                    return;
                }
            }

            base.OK();
            PersistentDataManager.UserPreferences.Save();

            if (memoryLimitChanged)
            {
                DataManager.ConfigureMemoryBudget(
                    PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit,
                    SystemInfo.systemMemorySize);
            }

            if (normalizationChanged)
            {
                int memoryCacheLimit = PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit;
                DataManager.ConfigureMemoryBudget(0, 0);
                try
                {
                    DataManager.ClearDerivedData();
                    if (ApplicationState.LoadedProject != null && Module3DMain.Scenes.Count > 0)
                    {
                        var visualizations = Module3DMain.PrepareReloadScenes();
                        await LoadingManager.LoadAsync((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
                    }
                }
                finally
                {
                    DataManager.ConfigureMemoryBudget(memoryCacheLimit, SystemInfo.systemMemorySize);
                }
            }

            UITools.ShowMemoryCacheBudgetWarningIfNeeded();
        }

        public override void Close()
        {
            DataManager.DefaultNormalization = m_InitialNormalization;
            base.Close();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">User pereferences to modify</param>
        protected override void SetFields(UserPreferences objectToDisplay)
        {
            // General
            m_ProjectPreferencesSubModifier.Object = objectToDisplay.General.Project;
            m_ThemePreferencesSubModifier.Object = objectToDisplay.General.Theme;
            m_LocationPreferencesSubModifier.Object = objectToDisplay.General.Localization;
            m_SystemPreferencesSubModifier.Object = objectToDisplay.General.System;
            m_MiscPreferencesSubModifier.Object = objectToDisplay.General.Misc;

            // Data
            m_EEGPreferencesSubModifier.Object = objectToDisplay.Data.EEG;
            m_ProtocolPreferencesSubModifier.Object = objectToDisplay.Data.Protocol;
            m_AnatomyPreferencesModifier.Object = objectToDisplay.Data.Anatomic;
            m_AtlasesPreferencesSubModifier.Object = objectToDisplay.Data.Atlases;

            // Visualization
            m_3DPreferencesSubModifier.Object = objectToDisplay.Visualization._3D;
            m_TrialMatrixPreferencesSubModifier.Object = objectToDisplay.Visualization.TrialMatrix;
            m_GraphPreferencesSubModifier.Object = objectToDisplay.Visualization.Graph;
            m_CutPreferencesSubModifier.Object = objectToDisplay.Visualization.Cut;
        }
        #endregion
    }
}
