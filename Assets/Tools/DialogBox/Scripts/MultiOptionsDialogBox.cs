using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class MultiOptionsDialogBox : DialogBox
    {
        #region Properties
        [SerializeField] Button m_Button1;
        [SerializeField] Button m_Button2;
        #endregion

        #region Public Methods
        public void Open(string title, string message, UnityAction button1action, UnityAction button2action)
        {
            Open(title, message);
            if (button1action == null) button1action = new UnityAction(() => { });
            if (button2action == null) button2action = new UnityAction(() => { });
            m_Button1.onClick.AddListener(() => { button1action(); Close(); });
            m_Button2.onClick.AddListener(() => { button2action(); Close(); });
        }
        #endregion
    }
}