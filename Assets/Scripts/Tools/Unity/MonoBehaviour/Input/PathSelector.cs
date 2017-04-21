using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity
{
    public abstract class PathSelector : MonoBehaviour
    {
        #region Properties
        protected InputField m_inputfield;
        protected Button m_button;

        public InputField.OnChangeEvent onValueChanged { get { return m_inputfield.onValueChanged; } }
        public bool interactable { set { m_inputfield.interactable = value; m_button.interactable = value; } }
        public string Path { get { return m_inputfield.text; } set { m_inputfield.text = value; } }
        public string Message;
        #endregion

        #region Public Methods
        public virtual void Open()
        {
            string path = OpenDialog();
            if (!string.IsNullOrEmpty(path)) Path = path;
        }
        #endregion

        #region Private Methods
        protected void Awake()
        {
            m_inputfield = GetComponent<InputField>();
            m_inputfield.onValueChanged.AddListener((value) => ChangeValue(value));
            m_button = GetComponentInChildren<Button>();
            m_button.onClick.AddListener(() => Open());
        }
        protected virtual void ChangeValue(string value)
        {
            StringExtension.StandardizeToPath(ref value);
            m_inputfield.text = value;
        }
        protected abstract string OpenDialog();
        #endregion
    }
}
