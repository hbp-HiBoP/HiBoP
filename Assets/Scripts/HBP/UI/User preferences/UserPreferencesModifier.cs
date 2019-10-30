using Tools.Unity;
using UnityEngine;

namespace HBP.UI.UserPreferences
{
    public class UserPreferencesModifier : ObjectModifier<HBP.Data.Preferences.UserPreferences>
    {
        #region Properties
        [SerializeField] ProjectPreferencesSubModifier m_ProjectPreferencesSubModifier;
        [SerializeField] ThemePreferencesSubModifier m_ThemePreferencesSubModifier;
        [SerializeField] LocationPreferencesSubModifier m_LocationPreferencesSubModifier;
        [SerializeField] SystemPreferencesSubModifier m_SystemPreferencesSubModifier;
        [SerializeField] EEGPreferencesSubModifier m_EEGPreferencesSubModifier;
        [SerializeField] ProtocolPreferencesSubModifier m_ProtocolPreferencesSubModifier;
        [SerializeField] AnatomyPreferencesSubModifier m_AnatomyPreferencesModifier;
        [SerializeField] _3DPreferencesSubModifier m_3DPreferencesSubModifier;
        [SerializeField] TrialMatrixPreferencesSubModifier m_TrialMatrixPreferencesSubModifier;
        [SerializeField] GraphPreferencesSubModifier m_GraphPreferencesSubModifier;
        [SerializeField] CutPreferencesSubModifier m_CutPreferencesSubModifier;

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
                m_3DPreferencesSubModifier.Interactable = value;
                m_TrialMatrixPreferencesSubModifier.Interactable = value;
                m_GraphPreferencesSubModifier.Interactable = value;
                m_CutPreferencesSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override  void Save()
        {
            base.Save();
            ClassLoaderSaver.SaveToJSon(ApplicationState.UserPreferences, Data.Preferences.UserPreferences.PATH, true);
        }
        public override void Close()
        {
            Theme.Theme.UpdateThemeElements(ApplicationState.UserPreferences.Theme);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields(Data.Preferences.UserPreferences objectToDisplay)
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

            // Visualization
            m_3DPreferencesSubModifier.Object = objectToDisplay.Visualization._3D;
            m_TrialMatrixPreferencesSubModifier.Object = objectToDisplay.Visualization.TrialMatrix;
            m_GraphPreferencesSubModifier.Object = objectToDisplay.Visualization.Graph;
            m_CutPreferencesSubModifier.Object = objectToDisplay.Visualization.Cut;
        }
        #endregion
    }
}