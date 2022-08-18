using UnityEngine;
using HBP.Display.Preferences;
using HBP.Core.Data;
using HBP.UI.Tools;

namespace HBP.UI.Main.Preferences
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
        [SerializeField] EEGPreferencesSubModifier m_EEGPreferencesSubModifier;
        [SerializeField] ProtocolPreferencesSubModifier m_ProtocolPreferencesSubModifier;
        [SerializeField] AnatomyPreferencesSubModifier m_AnatomyPreferencesModifier;
        [SerializeField] AtlasesPreferencesSubModifier m_AtlasesPreferencesSubModifier;
        [SerializeField] _3DPreferencesSubModifier m_3DPreferencesSubModifier;
        [SerializeField] TrialMatrixPreferencesSubModifier m_TrialMatrixPreferencesSubModifier;
        [SerializeField] GraphPreferencesSubModifier m_GraphPreferencesSubModifier;
        [SerializeField] CutPreferencesSubModifier m_CutPreferencesSubModifier;

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
        public override  void OK()
        {
            base.OK();
            ApplicationState.UserPreferences.Save();
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
            m_LocationPreferencesSubModifier.Object = objectToDisplay.General.Location;
            m_SystemPreferencesSubModifier.Object = objectToDisplay.General.System;

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