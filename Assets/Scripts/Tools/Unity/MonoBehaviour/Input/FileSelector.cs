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
        public string Message;
        public string Extension;
        #endregion

        #region Public Methods
        public void Open()
        {
                string l_result = HBP.Module3D.DLL.QtGUI.GetExistingFileName(Extension.Split(','), Message, GetComponent<InputField>().text);
                if (l_result != string.Empty)
                {
                    StringExtension.StandardizeToPath(ref l_result);
                    GetComponent<InputField>().text = l_result;
                }
        }
        #endregion
    }
}

