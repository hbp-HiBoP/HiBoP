using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class SharedFMRIDataInfoSubModifier : SubModifier<Core.Data.SharedFMRIDataInfo>
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
        protected override void SetFields(Core.Data.SharedFMRIDataInfo objectToDisplay)
        {
            m_FileSelector.File = objectToDisplay.MaskDataContainer.SavedFile;
        }
        #endregion
    }
}