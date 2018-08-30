using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity
{
    public class FolderSelector : MonoBehaviour
    {
        #region Properties
        public InputField.OnChangeEvent onValueChanged { get { return m_Inputfield.onValueChanged; } }
        public bool interactable { set { m_Inputfield.interactable = value; m_Button.interactable = value; } }
        public string Folder { get { return m_Inputfield.text; } set { m_Inputfield.text = value; } }
        public string Message;

        [SerializeField] InputField m_Inputfield;
        [SerializeField] Button m_Button;
        #endregion

        #region Public Methods
        public void Open()
        {
            string l_result = HBP.Module3D.DLL.QtGUI.GetExistingDirectoryName( Message, m_Inputfield.text);
            if (l_result != string.Empty)
            {
                StringExtension.StandardizeToPath(ref l_result);
                m_Inputfield.text = l_result;
            }
        }
        #endregion
    }
}