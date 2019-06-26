using UnityEngine;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class SingleMeshSubModifier : SubModifier<SingleMesh>
    {
        #region Properties
        [SerializeField] FileSelector m_MeshFileSelector;
        [SerializeField] FileSelector m_MarsAtlasFileSelector;

        public override bool Interactable
        {
            get { return base.Interactable; }
            set
            {
                base.Interactable = value;

                m_MeshFileSelector.interactable = value;
                m_MarsAtlasFileSelector.interactable = value;
            }
        }
        public override SingleMesh Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                m_MeshFileSelector.File = value.SavedPath;
                m_MarsAtlasFileSelector.File = value.SavedMarsAtlasPath;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_MeshFileSelector.onValueChanged.AddListener((path) => m_Object.Path = path);
            m_MarsAtlasFileSelector.onValueChanged.AddListener((marsAtlasPath) => m_Object.MarsAtlasPath = marsAtlasPath);
        }
        #endregion
    }
}