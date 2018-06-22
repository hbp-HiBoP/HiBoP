using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity
{
    public class FileSelector : MonoBehaviour
    {
        #region Properties
        public InputField.OnChangeEvent onValueChanged { get { return GetComponent<InputField>().onValueChanged; } }
        public bool interactable { set { GetComponent<InputField>().interactable = value; GetComponentInChildren<Button>().interactable = value; } }
        public string File { get { return GetComponent<InputField>().text; } set { GetComponent<InputField>().text = value; } }
        public string DefaultDirectory{ get; set; }
        public string Message;
        public string Extension;
        #endregion

        #region Public Methods
        public void Open()
        {
            string path = GetComponent<InputField>().text;
            if (!string.IsNullOrEmpty(path))
            {
                FileInfo file = new FileInfo(path);
                if (!file.Exists || file.Extension == Extension) path = DefaultDirectory;
            }
            else
            {
                path = DefaultDirectory;
            }
            string l_result = HBP.Module3D.DLL.QtGUI.GetExistingFileName(Extension.Split(','), Message, path);
            if (l_result != string.Empty)
            {
                l_result = l_result.StandardizeToPath();
                GetComponent<InputField>().text = l_result;
            }
        }
        #endregion
    }
}

