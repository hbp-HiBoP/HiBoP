using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SceneBarElement : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;

        [SerializeField]
        private Text m_Text;
        [SerializeField]
        private Toggle m_Toggle;
        [SerializeField]
        private Button m_Button;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_Text.text = scene.Name;
            m_Button.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.RemoveScene(scene);
                Destroy(gameObject);
            });
            m_Toggle.isOn = true;
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                scene.OnChangeVisibleState.Invoke(isOn);
            });
        }
        #endregion
    }
}