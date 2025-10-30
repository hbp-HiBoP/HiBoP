using HBP.Core.Data;
using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class TagSelectionItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_Text;
        [SerializeField] Toggle m_Toggle;
        
        public bool Selected
        {
            get
            {
                return m_Toggle.isOn;
            }
            set
            {
                m_Toggle.SetValue(value);
            }
        }
        #endregion

        #region Events
        public Toggle.ToggleEvent OnValueChanged => m_Toggle.onValueChanged;
        #endregion

        #region Public Methods
        public void Set(BaseTag tag)
        {
            m_Text.text = tag.Name;
        }
        #endregion
    }
}