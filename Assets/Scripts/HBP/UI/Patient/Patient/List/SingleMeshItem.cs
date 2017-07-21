using UnityEngine;
using HBP.Data.Anatomy;
using System;

namespace HBP.UI.Anatomy
{
    public class SingleMeshItem : MeshItem
    {
        #region Properties
        [SerializeField] Tools.Unity.FileSelector m_MeshFileSelector;
        public override bool interactable
        {
            get
            {
                return base.interactable;
            }

            set
            {
                base.interactable = value;
                m_MeshFileSelector.interactable = value;
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
        public override Type GetObjectType()
        {
            return typeof(SingleMesh);
        }
        #endregion

        #region Protected Methods
        protected override void SetObject(Data.Anatomy.Mesh objectToSet)
        {
            base.SetObject(objectToSet);
            SingleMesh mesh = Object as SingleMesh;
            m_MeshFileSelector.File = mesh.Path;
        }
        #endregion
    }
}