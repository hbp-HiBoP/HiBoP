using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI
{
    public class MultiOptionsDialogBox : DialogBox
    {
        #region Properties
        [SerializeField] Button m_Button1;
        [SerializeField] Button m_Button2;
        #endregion

        #region Public Methods
        public void Open(string title, string message, UnityAction button1action, string button1name, UnityAction button2action, string button2name)
        {
            Open(title, message);
            if (button1action == null) button1action = new UnityAction(() => { });
            if (button2action == null) button2action = new UnityAction(() => { });
            m_Button1.onClick.AddListener(() => { button1action(); Close(); });
            m_Button2.onClick.AddListener(() => { button2action(); Close(); });
            if (!string.IsNullOrEmpty(button1name)) m_Button1.GetComponentInChildren<Text>().text = button1name;
            if (!string.IsNullOrEmpty(button2name)) m_Button2.GetComponentInChildren<Text>().text = button2name;
        }
        #endregion
    }
}