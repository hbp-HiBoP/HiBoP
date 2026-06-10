using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BlocNamingItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_CodeText;
        [SerializeField] private Text m_OccurrencesText;
        [SerializeField] private InputField m_NameInputField;

        public int Code { get; private set; }
        public int Occurrences { get; private set; }
        public string BlocName => m_NameInputField.text;
        #endregion

        #region Events
        [HideInInspector] public UnityEvent OnNameChanged = new();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_NameInputField.onValueChanged.AddListener((value) => OnNameChanged.Invoke());
        }
        #endregion

        #region Public Methods
        public void SetData(int code, int occurrences, string initialName = "")
        {
            Code = code;
            Occurrences = occurrences;
            
            m_CodeText.text = code.ToString();
            m_OccurrencesText.text = $"({occurrences} occurences)";
            m_NameInputField.text = initialName;
        }
        #endregion
    }
}