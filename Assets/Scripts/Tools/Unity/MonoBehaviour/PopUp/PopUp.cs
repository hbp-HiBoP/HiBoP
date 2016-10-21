using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Tools.Unity
{
    public class PopUp : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Text m_title;

        [SerializeField]
        Text m_message;
        #endregion

        #region Public Methods
        public void Show(string message, string title)
        {
            gameObject.SetActive(true);
            m_title.text = title;
            m_message.text = message;
        }
        public void Show(string message)
        {
            Show(message, "Error");
        }
        public void Close()
        {
            GameObject.Destroy(this.gameObject);
        }
        #endregion
    }
}

