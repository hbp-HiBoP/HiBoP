using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleSwapIcon : MonoBehaviour
    {
        #region Properties
        private Toggle m_Toggle;
        [SerializeField]
        private Image m_Icon;

        [SerializeField]
        private Sprite m_IsOnSprite;
        [SerializeField]
        private Sprite m_IsOffSprite;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    m_Icon.sprite = m_IsOnSprite;
                }
                else
                {
                    m_Icon.sprite = m_IsOffSprite;
                }
            });
        }
        #endregion
    }
}