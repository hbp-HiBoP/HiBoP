using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity
{
    public class FileSelector : MonoBehaviour
    {
        #region Properties
        public string Message;
        public string Extension;

        public InputField.OnChangeEvent onValueChanged
        {
            get
            {
                return m_InputField.onValueChanged;
            }
        }
        public bool interactable
        {
            set
            {
                m_InputField.interactable = value;
                m_OpenFileBrowserButton.interactable = value;
            }
        }
        public string File
        {
            get
            {
                return m_InputField.text;
            }
            set
            {
                m_InputField.text = value;
            }
        }

        [SerializeField] InputField m_InputField;
        [SerializeField] Button m_OpenFileBrowserButton;
        #endregion

        #region Public Methods
        public void Open()
        {
            string result = HBP.UI.FileBrowser.GetExistingFileName(Extension.Split(','), Message, m_InputField.text.ConvertToFullPath());
            if (result != string.Empty)
            {
                result = result.StandardizeToPath();
                m_InputField.text = result.ConvertToShortPath();
            }
        }
        #endregion
    }
}

