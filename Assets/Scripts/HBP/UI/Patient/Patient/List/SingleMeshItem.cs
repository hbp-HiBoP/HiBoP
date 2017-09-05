using UnityEngine;
using HBP.Data.Anatomy;
using System;

namespace HBP.UI.Anatomy
{
    public class SingleMeshItem : MeshItem
    {
        #region Properties
        [SerializeField] Tools.Unity.FileSelector m_MeshFileSelector;
        [SerializeField] Tools.Unity.FileSelector m_MarsAtlasFileSelector;
        public override Data.Anatomy.Mesh Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                SingleMesh singleMesh = value as SingleMesh;
                m_MeshFileSelector.File = singleMesh.Path;
                m_MarsAtlasFileSelector.File = singleMesh.MarsAtlasPath;
            }
        }
        public override Type Type
        {
            get
            {
                return typeof(SingleMesh);
            }
        }
        public new bool interactable
        {
            get
            {
                return base.interactable;
            }

            set
            {
                base.interactable = value;
                m_MeshFileSelector.interactable = value;
                m_MarsAtlasFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            SingleMesh mesh = Object as SingleMesh;
            mesh.Path = m_MeshFileSelector.File;
            mesh.MarsAtlasPath = m_MarsAtlasFileSelector.File;
        }
        #endregion
    }
}