using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class CodeSelectionItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_CodeText;
        [SerializeField] private Text m_OccurrencesText;
        [SerializeField] private Toggle m_Toggle;

        public int Code { get; private set; }
        public int Occurrences { get; private set; }
        public bool IsSelected => m_Toggle.isOn;
        #endregion

        #region Events
        public Toggle.ToggleEvent OnSelectionChanged => m_Toggle.onValueChanged;
        #endregion

        #region Public Methods
        public void SetData(int code, int occurrences, bool isSelected = false)
        {
            Code = code;
            Occurrences = occurrences;
            
            m_CodeText.text = code.ToString();
            m_OccurrencesText.text = $"({occurrences} occurences)";
            m_Toggle.isOn = isSelected;
        }
        #endregion
    }
}