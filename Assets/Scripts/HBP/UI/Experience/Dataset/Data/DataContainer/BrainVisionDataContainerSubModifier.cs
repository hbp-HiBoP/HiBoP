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

        public override container.BrainVision Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_FileSelector.File = value.SavedHeader;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_FileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_FileSelector.onValueChanged.AddListener((header) => { m_Object.Header = header; });
        }
        #endregion
    }
}