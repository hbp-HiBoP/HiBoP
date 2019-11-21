using Tools.Unity;
using UnityEngine;
using container = HBP.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class EDFDataContainerSubModifier : SubModifier<container.EDF>
    {
        #region Properties
        [SerializeField] FileSelector m_FileSelector;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_FileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_FileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Preferences.LocalizerDatabase;
            m_FileSelector.onValueChanged.AddListener((path) => { Object.Path = path; });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(container.EDF objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedEDF;
        }
        #endregion
    }
}