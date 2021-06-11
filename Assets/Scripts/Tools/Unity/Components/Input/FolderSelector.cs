using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity
{
    public class FolderSelector : MonoBehaviour
    {
        #region Properties
        public InputField.OnChangeEvent onValueChanged { get { return m_Inputfield.onValueChanged; } }
        public InputField.SubmitEvent onEndEdit { get { return m_Inputfield.onEndEdit; } }
        public bool interactable { set { m_Inputfield.interactable = value; m_Button.interactable = value; } }
        public string Folder { get { return m_Inputfield.text; } set { m_Inputfield.text = value; } }
        public string Message;

        [SerializeField] InputField m_Inputfield;
        [SerializeField] Button m_Button;
        #endregion

        #region Public Methods
        public void Open()
        {
#if UNITY_STANDALONE_OSX
            HBP.UI.FileBrowser.GetExistingDirectoryNameAsync((result) =>
            {
                result = result.StandardizeToPath();
                m_Inputfield.text = result;
                m_Inputfield.onEndEdit.Invoke(result);
            }, Message, m_Inputfield.text);
#else
            string result = HBP.UI.FileBrowser.GetExistingDirectoryName( Message, m_Inputfield.text);
            if (result != string.Empty)
            {
                result = result.StandardizeToPath();
                m_Inputfield.text = result;
                m_Inputfield.onEndEdit.Invoke(result);
            }
#endif
        }
        #endregion
    }
}