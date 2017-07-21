using UnityEngine;
using HBP.Data.Anatomy;
using System;

namespace HBP.UI.Anatomy
{
    public class SingleMeshItem : MeshItem
    {
        #region Properties
        [SerializeField] Tools.Unity.FileSelector m_MeshFileSelector;
        public override Data.Anatomy.Mesh Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_MeshFileSelector.File = (value as SingleMesh).Path;
            }
        }
        public override Type Type
        {
            get
            {
                return typeof(SingleMesh);
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            SingleMesh mesh = Object as SingleMesh;
            mesh.Path = m_MeshFileSelector.File;
        }
        #endregion
    }
}