using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    [RequireComponent(typeof(Image))]
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float m_percentage = 0.0f;
        public float Percentage
        {
            get { return m_percentage; }
            set
            {
                float l_percentage = value;
                if(l_percentage < 0)
                {
                    l_percentage = 0;
                }
                else if(l_percentage > 1)
                {
                    l_percentage = 1;
                }
                m_percentage = l_percentage; m_checkMark.rectTransform.anchorMax = new Vector2(l_percentage, 1); m_checkMark.rectTransform.sizeDelta = new Vector2(0, 0); m_text.text = iPercentage.ToString() + "%";
            }
        }
        public int iPercentage
        {
            get
            {
                return (int)(Percentage * 100.0f);
            }
            set
            {
                Percentage = value / 100.0f;
            }
        }
        public float fPercentage
        {
            get
            {
                return Percentage * 100.0f;
            }
            set
            {
                Percentage = value / 100.0f;
            }
        }

        Image m_backGround;
        Image m_checkMark;
        Text m_text;

        public Color BackGroundColor
        {
            get { return m_backGround.color; }
            set { m_backGround.color = value; }
        }
        public Color CheckMarkColor
        {
            get { return m_checkMark.color; }
            set { m_checkMark.color = value; }
        }
        public Color TextColor
        {
            get { return m_text.color; }
            set { m_text.color = value; }
        }

        void OnEnable()
        {
            Set();
        }

        public void Set()
        {
            m_backGround = GetComponent<Image>();
            m_checkMark = transform.GetChild(0).GetComponent<Image>();
            m_text = transform.GetChild(1).GetComponent<Text>();
        }
    }
}