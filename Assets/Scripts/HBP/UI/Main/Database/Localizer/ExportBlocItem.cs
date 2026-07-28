using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ExportBlocItem : MonoBehaviour
    {
        #region Properties

        [SerializeField] private Text m_BlocNameText;
        [SerializeField] private Toggle m_Toggle;

        public string Name => m_BlocNameText.text;
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

        public void Initialize(string blocName)
        {
            m_BlocNameText.text = blocName;
            m_Toggle.isOn = false;
        }

        #endregion
    }
}
