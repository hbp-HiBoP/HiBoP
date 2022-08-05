using HBP.Module3D;
using UnityEngine;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Small bar at the bottom of the UI that displays all open scenes and allows to hide or close some of them
    /// </summary>
    public class SceneBar : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Prefab of the object on the scene bar that allows to hide or close a scene
        /// </summary>
        [SerializeField] private GameObject m_SceneBarElementPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            HBP3DModule.OnAddScene.AddListener((scene) =>
            {
                SceneBarElement element = Instantiate(m_SceneBarElementPrefab, transform).GetComponent<SceneBarElement>();
                element.Initialize(scene);
            });
        }
        #endregion
    }
}