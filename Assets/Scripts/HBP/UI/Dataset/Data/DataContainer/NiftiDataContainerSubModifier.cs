using UnityEngine;
using HBP.Core.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class NiftiDataContainerSubModifier : SubModifier<Nifti>
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
        protected override void SetFields(Nifti objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedFile;
        }
        #endregion
    }
}