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
        public string DefaultDirectory;

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
            string path = m_InputField.text;
            if (!string.IsNullOrEmpty(path))
            {
                FileInfo file = new FileInfo(path);
                if (!file.Exists || file.Extension == Extension) path = DefaultDirectory;
            }
            else
            {
                path = DefaultDirectory;
            }

            string result = HBP.Module3D.DLL.QtGUI.GetExistingFileName(Extension.Split(','), Message, path);
            if (result != string.Empty)
            {
                result = result.StandardizeToPath();
                m_InputField.text = result.ConvertToShortPath();
            }
        }
        #endregion
    }
}

