using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class DialogBox : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Text m_message;
        #endregion

        #region Public Methods
        public void Open(string message)
        {
            RectTransform rect = (transform as RectTransform);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
            m_message.text = message;
            LayoutElement texteLayoutElement = m_message.GetComponent<LayoutElement>();
            texteLayoutElement.preferredWidth = Mathf.Min(m_message.preferredWidth, texteLayoutElement.preferredWidth);
        }
        public void Close()
        {
            Destroy(gameObject);
        }
        #endregion
    }
}

