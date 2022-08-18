using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(Image))]
    public class ImageColorSwitcher : MonoBehaviour
    {
        #region Properties
        public Color[] Colors;
        Image m_image;
        #endregion

        #region Initialisation
        void Awake()
        {
            m_image = GetComponent<Image>();
        }
        #endregion

        #region Public Methods
        public void Set(int i)
        {
            if (i >= 0 && i < Colors.Length)
            {
                m_image.color = Colors[i];
            }
            else
            {
                m_image.color = Color.black;
            }
        }
        #endregion
    }
}