using UnityEngine;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class LeftRightMeshGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] FileSelector m_LeftMeshFileSelector;
        [SerializeField] FileSelector m_RightMeshFileSelector;

        [SerializeField] FileSelector m_LeftMarsAtlasFileSelector;
        [SerializeField] FileSelector m_RightMarsAtlasFileSelector;
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(LeftRightMesh mesh)
        {
            m_LeftMeshFileSelector.File = mesh.LeftHemisphere;
            m_LeftMeshFileSelector.onValueChanged.RemoveAllListeners();
            m_LeftMeshFileSelector.onValueChanged.AddListener((path) => mesh.LeftHemisphere = path);

            m_RightMeshFileSelector.File = mesh.RightHemisphere;
            m_RightMeshFileSelector.onValueChanged.RemoveAllListeners();
            m_RightMeshFileSelector.onValueChanged.AddListener((path) => mesh.RightHemisphere = path);

            m_LeftMarsAtlasFileSelector.File = mesh.LeftMarsAtlasHemisphere;
            m_LeftMarsAtlasFileSelector.onValueChanged.RemoveAllListeners();
            m_LeftMarsAtlasFileSelector.onValueChanged.AddListener((path) => mesh.LeftMarsAtlasHemisphere = path);

            m_RightMarsAtlasFileSelector.File = mesh.LeftMarsAtlasHemisphere;
            m_RightMarsAtlasFileSelector.onValueChanged.RemoveAllListeners();
            m_RightMarsAtlasFileSelector.onValueChanged.AddListener((path) => mesh.RightMarsAtlasHemisphere = path);
        }
        #endregion

        #region Private Methods

        #endregion
    }
}