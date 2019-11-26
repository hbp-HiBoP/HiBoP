using Tools.Unity;
using UnityEngine;
using container = HBP.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class BrainVisionDataContainerSubModifier : SubModifier<container.BrainVision>
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
        protected override void SetFields(container.BrainVision objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.SavedHeader;
        }
        #endregion
    }
}