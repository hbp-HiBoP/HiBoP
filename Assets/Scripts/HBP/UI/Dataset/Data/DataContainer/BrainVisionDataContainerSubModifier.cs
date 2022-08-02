using Tools.Unity;
using UnityEngine;
using HBP.Core.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class BrainVisionDataContainerSubModifier : SubModifier<BrainVision>
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
            m_FileSelector.onValueChanged.AddListener((header) => { Object.Header = header; });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(BrainVision objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedHeader;
        }
        #endregion
    }
}