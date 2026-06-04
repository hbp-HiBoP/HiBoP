using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BlocToggleItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_BlocNameText;
        [SerializeField] private Toggle m_Toggle;

        public int MainCode { get; private set; }
        public string BlocName { get; private set; }
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
        public void SetData(int mainCode, string blocName, bool isSelected = false)
        {
            MainCode = mainCode;
            BlocName = blocName;
            
            m_BlocNameText.text = blocName;
            m_Toggle.isOn = isSelected;
        }
        public void SetDataWithBlocName(string blocName, bool isSelected = false)
        {
            MainCode = -1; // Not used in bloc name mode
            BlocName = blocName;
            
            m_BlocNameText.text = blocName;
            m_Toggle.isOn = isSelected;
        }
        #endregion
    }
}