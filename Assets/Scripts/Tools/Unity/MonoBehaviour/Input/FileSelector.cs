using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity
{
    public class FileSelector : MonoBehaviour
    {
        #region Properties
        InputField m_inputfield;
        Button m_button;

        public InputField.OnChangeEvent onValueChanged { get { return m_inputfield.onValueChanged; } }
        public bool interactable { set { m_inputfield.interactable = value;  m_button.interactable = value; } }
        public string File { get { return m_inputfield.text; } set { m_inputfield.text = value; } }
        public string Message;
        public string Extension;
        #endregion

        #region Public Methods
        public void Open()
        {
                string l_result = HBP.VISU3D.DLL.QtGUI.getOpenFileName(new string[] { Extension }, Message, m_inputfield.text);
                if (l_result != string.Empty)
                {
                    l_result.StandardizeToPath();
                    m_inputfield.text = l_result;
                }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_inputfield = GetComponent<InputField>();
            m_button = GetComponentInChildren<Button>();
        }
        #endregion
    }
}

