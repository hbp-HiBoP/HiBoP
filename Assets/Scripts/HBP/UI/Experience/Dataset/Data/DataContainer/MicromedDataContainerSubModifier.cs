using Tools.Unity;
using UnityEngine;
using container = HBP.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class MicromedDataContainerSubModifier : SubModifier<container.Micromed>
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
            m_FileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_FileSelector.onValueChanged.AddListener((path) => { Object.Path = path; });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(container.Micromed objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedPath;
        }
        #endregion
    }
}