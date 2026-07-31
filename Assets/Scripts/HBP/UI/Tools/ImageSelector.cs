using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(Button))]
    public class ImageSelector : MonoBehaviour
    {
        #region Properties

        [HideInInspector] public GenericEvent<string> onValueChanged = new();

        public bool interactable
        {
            get { return GetComponent<Button>().interactable; }
            set { GetComponent<Button>().interactable = value; }
        }

        string m_Path = string.Empty;

        public string Path
        {
            get { return m_Path; }
            set
            {
                if (m_Path != value)
                {
                    m_Path = value;
                    onValueChanged.Invoke(value);
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

        public async void Open()
        {
            string result = await FileBrowser.GetExistingFileNameAsync(EXTENSIONS, Message, m_Path);
            if (result != string.Empty)
            {
                result = result.StandardizeToPath();
                Path = result;
            }
        }

        void LoadImage(string path)
        {
            if (SpriteExtension.LoadSpriteFromFile(out Sprite sprite, path)) m_Image.sprite = sprite;
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
