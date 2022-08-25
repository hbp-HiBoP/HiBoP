using UnityEngine;
using HBP.Core.Data.Container;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class FIFDataContainerSubModifier : SubModifier<FIF>
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
        protected override void SetFields(FIF objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedFile;
        }
        #endregion
    }
}