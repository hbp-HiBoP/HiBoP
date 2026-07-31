using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Object3D;

namespace HBP.UI.Informations
{
    public class BlocItem : MonoBehaviour
    {
        #region Properties

        [SerializeField] private Text m_BlocNameText;
        [SerializeField] private Toggle m_Toggle;

        public string Name => m_BlocNameText.text;
        public bool IsSelected => m_Toggle.isOn;

        #endregion

        #region Public Methods

        public void Initialize(string blocName)
        {
            m_BlocNameText.text = blocName;
            m_Toggle.isOn = false;
        }

        #endregion
    }
}
