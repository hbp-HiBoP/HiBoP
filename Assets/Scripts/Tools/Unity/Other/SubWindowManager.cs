using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI
{
    [Serializable]
    public class SubWindowsManager
    {
        public UnityEvent<Window> OnOpenSubWindow { get; protected set; } = new GenericEvent<Window>();
        public UnityEvent<Window> OnCloseSubWindow { get; protected set; } = new GenericEvent<Window>();

        [SerializeField] protected List<Window> m_Windows = new List<HBP.UI.Window>();
        public ReadOnlyCollection<Window> Windows
        {
            get
            {
                return new ReadOnlyCollection<Window>(m_Windows);
            }
        }

        public void SaveAll()
        {
            SavableWindow[] windowsToSave = m_Windows.OfType<SavableWindow>().ToArray();
            foreach (var window in windowsToSave) window.Save();
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
            OnOpenSubWindow.Invoke(window);
        }
        public void Remove(Window window)
        {
            m_Windows.Remove(window);
            OnCloseSubWindow.Invoke(window);
        }
    }
}