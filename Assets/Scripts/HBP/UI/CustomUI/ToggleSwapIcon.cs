using UnityEngine;
using UnityEngine.UI;

namespace HBP.Display.UI.Tools
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleSwapIcon : MonoBehaviour
    {
        #region Properties
        private Toggle m_Toggle;
        [SerializeField]
        private Image m_OnIcon;
        [SerializeField]
        private Image m_OffIcon;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                m_OffIcon.gameObject.SetActive(!isOn);
                m_OnIcon.gameObject.SetActive(isOn);
            });
        }
        #endregion
    }
}