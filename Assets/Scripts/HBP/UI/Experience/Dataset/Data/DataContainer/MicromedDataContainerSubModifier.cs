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

        public override container.Micromed Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                m_FileSelector.File = value.SavedPath;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_FileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_FileSelector.onValueChanged.AddListener((path) => { m_Object.Path = path; });
        }
        #endregion
    }
}