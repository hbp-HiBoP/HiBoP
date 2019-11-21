using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI
{
    [Serializable]
    public class WindowsReferencer
    {
        public UnityEvent<Window> OnOpenWindow { get; protected set; } = new GenericEvent<Window>();
        public UnityEvent<Window> OnCloseWindow { get; protected set; } = new GenericEvent<Window>();

        [SerializeField] protected List<Window> m_Windows = new List<Window>();
        public ReadOnlyCollection<Window> Windows
        {
            get
            {
                return new ReadOnlyCollection<Window>(m_Windows);
            }
        }

        public void SaveAll()
        {
            DialogWindow[] windowsToSave = m_Windows.OfType<DialogWindow>().ToArray();
            foreach (var window in windowsToSave) window.OK();
        }
        public void CloseAll()
        {
            Window[] windowsToClose = m_Windows.ToArray();
            foreach (var window in windowsToClose) window.Close();
        }
        public void Add(Window window)
        {
            m_Windows.Add(window);
            window.OnClose.AddListener(() => Remove(window));
            OnOpenWindow.Invoke(window);
        }
        public void Remove(Window window)
        {
            m_Windows.Remove(window);
            OnCloseWindow.Invoke(window);
        }
    }
}