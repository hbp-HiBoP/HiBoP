using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FMRIDataInfoSubModifier : SubModifier<Core.Data.FMRIDataInfo>
    {
        #region Properties
        [SerializeField] FileSelector m_FileSelector;
        public override bool Interactable
        {
            get
            {
                return m_Interactable;
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
            m_FileSelector.onValueChanged.AddListener((path) => { Object.MaskDataContainer.File = path; });
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.FMRIDataInfo objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.MaskDataContainer.SavedFile;
        }
        #endregion
    }
}