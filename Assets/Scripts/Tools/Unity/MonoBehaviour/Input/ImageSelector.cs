using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Tools.CSharp;

namespace Tools.Unity
{
    [RequireComponent(typeof(Button))]
    public class ImageSelector : MonoBehaviour
    {
        #region Properties
        [HideInInspector] public UnityEvent onValueChanged = new UnityEvent();
        public bool interactable
        {
            get
            {
                return GetComponent<Button>().interactable;
            }
            set
            {
                GetComponent<Button>().interactable = value;
            }
        }
        string m_Path = string.Empty;
        public string Path
        {
            get
            {
                return m_Path;
            }
            set
            {
                if(m_Path != value)
                {
                    m_Path = value;
                    onValueChanged.Invoke();
                    LoadImage(value);
                }
            }
        }
        public string Message;

        static string[] EXTENSIONS = new string[] { "png", "jpg" };
        Image m_Image;
        Sprite m_Icon;
        #endregion

        #region Public Methods
        public void Open()
        {
            string l_result = HBP.Module3D.DLL.QtGUI.GetExistingFileName( EXTENSIONS, Message, m_Path);
            if (l_result != string.Empty)
            {
                StringExtension.StandardizeToPath(ref l_result);
                Path = l_result;
            }
        }
        void LoadImage(string path)
        {
            Sprite sprite = SpriteExtension.Load(path);
            if (sprite) m_Image.sprite = sprite;
            else m_Image.sprite = m_Icon;
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Image = GetComponent<Image>();
            m_Icon = m_Image.sprite;
        }
        #endregion
    }
}