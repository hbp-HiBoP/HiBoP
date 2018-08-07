using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public abstract class ItemGestion<T> : SavableWindow where T : ICloneable, ICopiable, new()
    {
        #region Properties
        protected SelectableListWithItemAction<T> m_List;
        protected System.Collections.Generic.List<T> m_Items = new System.Collections.Generic.List<T>();
        protected ReadOnlyCollection<T> Items
        {
            get { return new ReadOnlyCollection<T>(m_Items); }
        }
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_List.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            FindObjectOfType<MenuButtonState>().SetInteractables();
            base.Save();
        }
        public virtual void Create()
        {
            OpenModifier(new T(),true);
        }
        public virtual void Remove()
        {
            foreach(T item in m_List.ObjectsSelected) RemoveItem(item);
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            m_List.Initialize();
            m_List.OnAction.AddListener((item, i) => OpenModifier(item, Interactable));
            base.Initialize();
        }
        protected virtual void OpenModifier(T item,bool interactable)
        {
            m_List.DeselectAll();
            ItemModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnClose.AddListener(() => OnCloseModifier(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            m_SubWindows.Add(modifier);
        }
        protected virtual void OnCloseModifier(ItemModifier<T> modifier)
        {
            m_SubWindows.Remove(modifier);
        }
        protected virtual void OnSaveModifier(ItemModifier<T> modifier)
        {
            if(!Items.Contains(modifier.Item))
            {
                AddItem(modifier.Item);
            }
            else
            {
                m_List.UpdateObject(modifier.Item);
            }
        }
        protected virtual void AddItem(T item)
        {
            m_Items.Add(item);
            m_List.Add(item);
        }
        protected virtual void AddItem(IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                AddItem(item);
            }
        }
        protected virtual void RemoveItem(T item)
        {
            m_Items.Remove(item);
            m_List.Remove(item);
        }
        protected virtual void RemoveItem(IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                RemoveItem(item);
            }
        }
        #endregion
    }
}