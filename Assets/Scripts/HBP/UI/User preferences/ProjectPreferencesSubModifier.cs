using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using HBP.Data.Preferences;

namespace HBP.UI.UserPreferences
{
    public class ProjectPreferencesSubModifier : SubModifier<Core.Data.Preferences.ProjectPreferences>
    {
        #region Properties
        [SerializeField] InputField m_DefaultName;
        [SerializeField] FolderSelector m_DefaultLocation;
        [SerializeField] FolderSelector m_DefaultPatientDatabase;
        [SerializeField] FolderSelector m_DefaultLocalizerDatabase;
        [SerializeField] FolderSelector m_DefaultExportDatabase;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_DefaultName.interactable = value;
                m_DefaultLocation.interactable = value;
                m_DefaultPatientDatabase.interactable = value;
                m_DefaultLocalizerDatabase.interactable = value;
                m_DefaultExportDatabase.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_DefaultName.onValueChanged.AddListener(value => Object.DefaultName = value);
            m_DefaultLocation.onValueChanged.AddListener(value => Object.DefaultLocation = value);
            m_DefaultPatientDatabase.onValueChanged.AddListener(value => Object.DefaultPatientDatabase = value);
            m_DefaultLocalizerDatabase.onValueChanged.AddListener(value => Object.DefaultLocalizerDatabase = value);
            m_DefaultExportDatabase.onValueChanged.AddListener(value => Object.DefaultExportLocation = value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.Preferences.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_DefaultName.text = objectToDisplay.DefaultName;
            m_DefaultLocation.Folder = objectToDisplay.DefaultLocation;
            m_DefaultPatientDatabase.Folder = objectToDisplay.DefaultPatientDatabase;
            m_DefaultLocalizerDatabase.Folder = objectToDisplay.DefaultLocalizerDatabase;
            m_DefaultExportDatabase.Folder = objectToDisplay.DefaultExportLocation;
        }
        #endregion
    }
}