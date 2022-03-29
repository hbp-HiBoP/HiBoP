using HBP.Module3D;
using Theme.Components;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Element of the scene bar used to hide/display or close an open scene
    /// </summary>
    public class SceneBarElement : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated logical scene
        /// </summary>
        private Base3DScene m_Scene;

        /// <summary>
        /// Displays the name of the scene
        /// </summary>
        [SerializeField] private Text m_Text;
        /// <summary>
        /// Toggle used to hide/display the scene
        /// </summary>
        [SerializeField] private Toggle m_Toggle;
        /// <summary>
        /// Button used to close the scene
        /// </summary>
        [SerializeField] private Button m_Button;

        /// <summary>
        /// Theme Element state used when the scene is selected
        /// </summary>
        [SerializeField] private State m_SelectedState;
        /// <summary>
        /// Corresponding theme element (to display when the scene is selected)
        /// </summary>
        [SerializeField] private ThemeElement m_ThemeElement;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the scene bar element
        /// </summary>
        /// <param name="scene">Associated logical scene</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            ApplicationState.Module3D.OnSelectScene.AddSafeListener((s) => SetSelectedState(s == m_Scene), gameObject);
            SetSelectedState(m_Scene.IsSelected);

            m_Text.text = scene.Name;

            m_Button.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.RemoveScene(scene);
            });

            ApplicationState.Module3D.OnRemoveScene.AddSafeListener((s) =>
            {
                if (s == scene)
                {
                    Destroy(gameObject);
                }
            }, gameObject);

            m_Toggle.isOn = true;
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                scene.UpdateVisibleState(isOn);
            });
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set the selected or unselected state of the scene bar element
        /// </summary>
        /// <param name="selected">True if the scene bar element should appear as selected</param>
        private void SetSelectedState(bool selected)
        {
            if (selected)
            {
                m_ThemeElement.Set(m_SelectedState);
            }
            else
            {
                m_ThemeElement.Set();
            }
        }
        #endregion
    }
}