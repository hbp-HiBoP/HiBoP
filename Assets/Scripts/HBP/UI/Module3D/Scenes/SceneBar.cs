using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SceneBar : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private GameObject m_SceneBarElementPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            ApplicationState.Module3D.OnAddScene.AddListener((scene) =>
            {
                SceneBarElement element = Instantiate(m_SceneBarElementPrefab, transform).GetComponent<SceneBarElement>();
                element.Initialize(scene);
            });
        }
        #endregion
    }
}