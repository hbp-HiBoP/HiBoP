using Tools.Unity;
using UnityEngine;
using container = HBP.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class NiftiDataContainerSubModifier : SubModifier<container.Nifti>
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
            m_FileSelector.onValueChanged.AddListener((path) => { Object.File = path; });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(container.Nifti objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedFile;
        }
        #endregion
    }
}