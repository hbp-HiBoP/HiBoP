using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ExportDataNameItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_DataNameText;
        [SerializeField] private Toggle m_Toggle;
        
        public string DataName => m_DataNameText.text;
        public bool IsSelected => m_Toggle.isOn;
        #endregion
        
        #region Events
        [HideInInspector] public UnityEvent OnToggleChanged = new();
        #endregion
        
        #region Private Methods
        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener((value) => OnToggleChanged.Invoke());
        }
        #endregion
        
        #region Public Methods
        public void Initialize(string dataName)
        {
            m_DataNameText.text = dataName;
            m_Toggle.isOn = false;
        }
        #endregion
    }
}