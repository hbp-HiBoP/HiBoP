using System;
using HBP.Data.Anatomy;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class LeftRightMeshItem : MeshItem
    {
        #region Properties
        [SerializeField] Tools.Unity.FileSelector m_LeftFileSelector;
        [SerializeField] Tools.Unity.FileSelector m_RightFileSelector;
        public override Data.Anatomy.Mesh Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                LeftRightMesh mesh = value as LeftRightMesh;
                m_LeftFileSelector.File = mesh.LeftHemisphere;
                m_RightFileSelector.File = mesh.RightHemisphere;
            }
        }
        public override Type Type
        {
            get
            {
                return typeof(LeftRightMesh);
            }
        }
        public override bool interactable
        {
            set
            {
                base.interactable = value;
                m_LeftFileSelector.interactable = value;
                m_RightFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            LeftRightMesh mesh = Object as LeftRightMesh;
            mesh.LeftHemisphere = m_LeftFileSelector.File;
            mesh.RightHemisphere = m_RightFileSelector.File;
        }
        #endregion
    }
}