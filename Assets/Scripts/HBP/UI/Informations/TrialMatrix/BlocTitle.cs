using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations.TrialMatrix
{
    public class BlocTitle : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_Title;
        public string Title
        {
            get => m_Title.text;
            set
            {
                m_Title.text = value;
                OnChangeTitleOrImage();
            }
        }
        [SerializeField] private Image m_Image;
        public Sprite Image
        {
            get => m_Image.sprite;
            set
            {
                m_Image.sprite = value;
                OnChangeTitleOrImage();
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Image.preserveAspect = true;
        }
        private void OnChangeTitleOrImage()
        {
            m_Title.gameObject.SetActive(m_Image.sprite == null);
            m_Image.gameObject.SetActive(m_Image.sprite != null);
        }
        #endregion
    }
}