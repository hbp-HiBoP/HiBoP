using UnityEngine;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class SingleMeshItem : MeshItem
    {
        #region Properties
        [SerializeField] Tools.Unity.FileSelector m_MeshFileSelector;
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            SingleMesh mesh = Object as SingleMesh;
            mesh.Path = m_MeshFileSelector.File;
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