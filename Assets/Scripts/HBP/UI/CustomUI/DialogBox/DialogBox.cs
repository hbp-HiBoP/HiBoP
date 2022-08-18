using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class DialogBox : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Text m_Message;
        [SerializeField]
        Text m_Title;
        #endregion

        #region Public Methods
        public void Open(string title, string message)
        {
            SetRect();
            SetMessages(title, message);

        }
        public void Close()
        {
            Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        void SetRect()
        {
            RectTransform rect = (transform as RectTransform);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
        }
        void SetMessages(string title, string message)
        {
            m_Title.text = title;
            m_Message.text = message;
            LayoutElement layoutElement = m_Message.transform.parent.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = Mathf.Min(Mathf.Max(m_Title.preferredWidth, m_Message.preferredWidth), layoutElement.preferredWidth);
        }
        #endregion
    }
}

