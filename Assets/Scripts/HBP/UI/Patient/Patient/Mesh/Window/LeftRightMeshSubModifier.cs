using UnityEngine;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class LeftRightMeshSubModifier : SubModifier<LeftRightMesh>
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
        public override LeftRightMesh Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                m_LeftMeshFileSelector.File = value.SavedLeftHemisphere;
                m_RightMeshFileSelector.File = value.SavedRightHemisphere;
                m_LeftMarsAtlasFileSelector.File = value.SavedLeftMarsAtlasHemisphere;
                m_RightMarsAtlasFileSelector.File = value.SavedRightMarsAtlasHemisphere;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_LeftMeshFileSelector.onValueChanged.AddListener((path) => m_Object.LeftHemisphere = path);
            m_RightMeshFileSelector.onValueChanged.AddListener((path) => m_Object.RightHemisphere = path);
            m_LeftMarsAtlasFileSelector.onValueChanged.AddListener((path) => m_Object.LeftMarsAtlasHemisphere = path);
            m_RightMarsAtlasFileSelector.onValueChanged.AddListener((path) => m_Object.RightMarsAtlasHemisphere = path);
        }
        #endregion
    }
}