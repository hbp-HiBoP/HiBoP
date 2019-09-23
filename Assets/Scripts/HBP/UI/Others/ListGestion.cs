using HBP.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
using Tools.CSharp;

namespace Tools.Unity.Components
{
    public abstract class ListGestion<T> : MonoBehaviour where T : ICloneable, ICopiable, new()
    {
        #region Properties
        protected List<T> m_Objects = new List<T>();
        public virtual List<T> Objects
        {
            get
            {
                return m_Objects;
            }
            set
            {
                m_Objects = value;
                List.Objects = new T[0]; 
                List.Add(m_Objects);
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
        public List<SavableWindow> SubWindows = new List<SavableWindow>();
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
            if(!Objects.Contains(item))
            {
                Objects.Add(item);
                List.Add(item);
            }
        }
        public virtual void UpdateItem(T item)
        {
            int index = Objects.FindIndex((t) => t.Equals(item));
            Objects[index] = item;
            List.UpdateObject(item);
        }
        public virtual void Remove(T item)
        {
            if(Objects.Contains(item))
            {
                Objects.Remove(item);
                List.Remove(item);
            }
            UpdateCounter();
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
            OpenCreatorWindow();
        }
        #endregion

        #region Private Methods
        protected virtual void OpenCreatorWindow()
        {
            CreatorWindow creatorWindow = ApplicationState.WindowsManager.Open<CreatorWindow>("Creator window", true);
            creatorWindow.IsLoadableFromFile = typeof(T).GetInterfaces().Contains(typeof(ILoadable<T>));
            creatorWindow.IsLoadableFromDatabase = typeof(T).GetInterfaces().Contains(typeof(ILoadableFromDatabase<T>));
            creatorWindow.OnSave.AddListener(() => OnSaveCreator(creatorWindow));
        }
        protected virtual void OpenSelector(T[] objects, bool multiSelection = false, bool openSelected = true, bool generateNewIDs = true)
        {
            ObjectSelector<T> selector = ApplicationState.WindowsManager.OpenSelector<T>();
            SubWindows.Add(selector);
            selector.OnClose.AddListener(() => OnCloseSubWindow(selector));
            selector.OnSave.AddListener(() => OnSaveSelector(selector, generateNewIDs));
            selector.Objects = objects;
            selector.MultiSelection = multiSelection;
            selector.OpenModifierWhenSave = openSelected;
            OnOpenSavableWindow.Invoke(selector);
            SubWindows.Add(selector);
        }
        protected virtual ItemModifier<T> OpenModifier(T item, bool interactable)
        {
            ItemModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnClose.AddListener(() => OnCloseSubWindow(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            OnOpenSavableWindow.Invoke(modifier);
            SubWindows.Add(modifier);
            return modifier;
        }
        protected virtual void OnCloseSubWindow(SavableWindow subWindow)
        {
            OnCloseSavableWindow.Invoke(subWindow);
            SubWindows.Remove(subWindow);
        }
        protected virtual void OnSaveModifier(ItemModifier<T> modifier)
        {
            if (!Objects.Contains(modifier.Item))
            {
                Add(modifier.Item);
            }
            else
            {
                UpdateItem(modifier.Item);
            }
            OnCloseSavableWindow.Invoke(modifier);
            SubWindows.Remove(modifier);
        }
        protected virtual void OnSaveCreator(CreatorWindow creatorWindow)
        {
            HBP.Data.Enums.CreationType type = creatorWindow.Type;
            T item = new T();
            switch (type)
            {
                case HBP.Data.Enums.CreationType.FromScratch:
                    OpenModifier(item, Interactable);
                    break;
                case HBP.Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(Objects.ToArray());
                    break;
                case HBP.Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item, Interactable);
                    }
                    break;
                case HBP.Data.Enums.CreationType.FromDatabase:
                    if(LoadFromDatabase(out T[] items))
                    {
                        OpenSelector(items, true, false, false);
                    }
                    break;
            }
        }
        protected virtual void OnSaveSelector(ObjectSelector<T> selector, bool generateNewIDs = true)
        {
            foreach(var selectedItem in selector.ObjectsSelected)
            {
                T cloneItem = (T)selectedItem.Clone();
                if(generateNewIDs)
                {
                    if (typeof(T).GetInterfaces().Contains(typeof(IIdentifiable)))
                    {
                        IIdentifiable identifiable = cloneItem as IIdentifiable;
                        identifiable.GenerateID();
                    }
                }
                if (cloneItem != null)
                {
                    if(selector.OpenModifierWhenSave)
                    {
                        OpenModifier(cloneItem, true);
                    }
                    else
                    {
                        Add(cloneItem);
                    }
                }
            }
            OnCloseSavableWindow.Invoke(selector);
            SubWindows.Remove(selector);
        }
        protected virtual bool LoadFromFile(out T result)
        {
            result = new T();
            ILoadable<T> loadable = result as ILoadable<T>;
            string path = FileBrowser.GetExistingFileName(new string[] { loadable.GetExtension() }).StandardizeToPath();
            if (path != string.Empty)
            {
                result = ClassLoaderSaver.LoadFromJson<T>(path);
                if (typeof(T).GetInterfaces().Contains(typeof(IIdentifiable)))
                {
                    IIdentifiable identifiable = result as IIdentifiable;
                    if (identifiable.ID == "xxxxxxxxxxxxxxxxxxxxxxxxx" || Objects.Any(p => (p as IIdentifiable).ID == identifiable.ID))
                    {

                        identifiable.ID = Guid.NewGuid().ToString();
                    }
                }
                return true;
            }
            return false;
        }
        protected virtual bool LoadFromDatabase(out T[] results)
        {
            string path = FileBrowser.GetExistingDirectoryName();
            ILoadableFromDatabase<T> loadable = new T() as ILoadableFromDatabase<T>;
            return loadable.LoadFromDatabase(path, out results);
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