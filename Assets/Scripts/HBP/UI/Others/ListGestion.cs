using HBP.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    public abstract class ListGestion<T> : MonoBehaviour where T : ICloneable, ICopiable, new()
    {
        #region Properties
        protected List<T> m_Items = new List<T>();
        public virtual List<T> Items
        {
            get
            {
                return m_Items;
            }
            set
            {
                m_Items = value;
                List.Objects = new T[0]; 
                List.Add(m_Items);
            }
        }
        protected Lists.SelectableListWithItemAction<T> List;
        protected bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                List.Interactable = value;
            }
        }
        public List<ItemModifier<T>> Modifiers = new List<ItemModifier<T>>();
        public SavableWindowEvent OnOpenSavableWindow = new SavableWindowEvent();
        public SavableWindowEvent OnCloseSavableWindow = new SavableWindowEvent();
        public Text Counter;
        #endregion

        #region Public Methods
        public virtual void Initialize()
        {
            List.Initialize();
            List.OnAction.AddListener((item, v) => OpenModifier(item, Interactable));
            List.OnSelectionChanged.AddListener(UpdateCounter);
        }
        public virtual void Initialize(List<HBP.UI.Window> windows)
        {
            Initialize();
            OnOpenSavableWindow.AddListener((window) => windows.Add(window));
            OnCloseSavableWindow.AddListener((window) => windows.Remove(window));
        }
        public virtual void Add(IEnumerable<T> items)
        {
            foreach (var item in items) Add(item);
        }
        public virtual void Add(T item)
        {
            if(!Items.Contains(item))
            {
                Items.Add(item);
                List.Add(item);
            }
        }
        public virtual void Remove(T item)
        {
            if(Items.Contains(item))
            {
                Items.Remove(item);
                List.Remove(item);
            }
        }
        public virtual void Remove(IEnumerable<T> items)
        {
            foreach (var item in items) Remove(item);
        }
        public virtual void RemoveSelected()
        {
            foreach (T item in List.ObjectsSelected) Remove(item);
        }
        public virtual void Create()
        {
            OpenModifier(new T(), m_Interactable);
        }
        #endregion

        #region Private Methods
        protected virtual void OpenModifier(T item, bool interactable)
        {
            ItemModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnClose.AddListener(() => OnCloseModifier(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            OnOpenSavableWindow.Invoke(modifier);
            Modifiers.Add(modifier);
        }
        protected virtual void OnCloseModifier(ItemModifier<T> modifier)
        {
            OnCloseSavableWindow.Invoke(modifier);
            Modifiers.Remove(modifier);
        }
        protected virtual void OnSaveModifier(ItemModifier<T> modifier)
        {
            if (!Items.Contains(modifier.Item))
            {
                Add(modifier.Item);
            }
            else
            {
                List.UpdateObject(modifier.Item);
            }
            OnCloseSavableWindow.Invoke(modifier);
            Modifiers.Remove(modifier);
        }
        protected virtual void UpdateCounter()
        {
            if(Counter != null)
            {
                Counter.text = List.ObjectsSelected.Length.ToString();
            }
        }
        #endregion
    }
}