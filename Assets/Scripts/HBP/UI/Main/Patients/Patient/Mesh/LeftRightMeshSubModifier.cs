using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class LeftRightMeshSubModifier : SubModifier<Core.Data.LeftRightMesh>
    {
        #region Properties
        [SerializeField] FileSelector m_LeftMeshFileSelector;
        [SerializeField] FileSelector m_RightMeshFileSelector;

        [SerializeField] FileSelector m_LeftMarsAtlasFileSelector;
        [SerializeField] FileSelector m_RightMarsAtlasFileSelector;

        public override bool Interactable
        {
            get { return base.Interactable; }
            set
            {
                base.Interactable = value;
                m_LeftMeshFileSelector.interactable = value;
                m_RightMeshFileSelector.interactable = value;
                m_LeftMarsAtlasFileSelector.interactable = value;
                m_RightMarsAtlasFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_LeftMeshFileSelector.onValueChanged.AddListener((path) => Object.LeftHemisphere = path);
            m_RightMeshFileSelector.onValueChanged.AddListener((path) => Object.RightHemisphere = path);
            m_LeftMarsAtlasFileSelector.onValueChanged.AddListener((path) => Object.LeftMarsAtlasHemisphere = path);
            m_RightMarsAtlasFileSelector.onValueChanged.AddListener((path) => Object.RightMarsAtlasHemisphere = path);
        }

        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.LeftRightMesh objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_LeftMeshFileSelector.File = objectToDisplay.SavedLeftHemisphere;
            m_RightMeshFileSelector.File = objectToDisplay.SavedRightHemisphere;
            m_LeftMarsAtlasFileSelector.File = objectToDisplay.SavedLeftMarsAtlasHemisphere;
            m_RightMarsAtlasFileSelector.File = objectToDisplay.SavedRightMarsAtlasHemisphere;
        }
        #endregion
    }
}