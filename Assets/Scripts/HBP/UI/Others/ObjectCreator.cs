using HBP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    [Serializable]
    public class ObjectCreator<T> : MonoBehaviour where T : ICloneable, ICopiable, new()
    {
        #region Properties
        [SerializeField] bool m_IsLoadableFromFile = true;
        public bool IsCreatableFromFile
        {
            get
            {
                return m_IsLoadableFromFile;
            }
            set
            {
                m_IsLoadableFromFile = value;            }
        }

        [SerializeField] bool m_IsLoadableFromDatabase = true;
        public bool IsCreatableFromDatabase
        {
            get
            {
                return m_IsLoadableFromDatabase;
            }
            set
            {
                m_IsLoadableFromDatabase = value;
            }
        }

        [SerializeField] bool m_IsCreatableFromScratch = true;
        public bool IsCreatableFromScratch
        {
            get
            {
                return m_IsCreatableFromScratch;
            }
            set
            {
                m_IsCreatableFromScratch = value;
            }
        }

        [SerializeField] bool m_IsCreatableFromExistingObjects = true;
        public bool IsCreatableFromExistingObjects
        {
            get
            {
                return m_IsCreatableFromExistingObjects;
            }
            set
            {
                m_IsCreatableFromExistingObjects = value;
            }
        }

        [SerializeField] List<T> m_ExistingItems = new List<T>();
        public List<T> ExistingItems
        {
            get
            {
                return m_ExistingItems;
            }
            set
            {
                m_ExistingItems = value;
            }
        } 

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        public virtual WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }

        public UnityEvent<T> OnObjectCreated { get; protected set; } = new GenericEvent<T>();
        #endregion

        #region Public Methods
        public virtual void Create()
        {
            bool createableFromScratch = IsCreatableFromScratch;
            bool createableFromFile = IsCreatableFromFile && typeof(T).GetInterfaces().Contains(typeof(ILoadable<T>));
            bool createableFromDatabase = IsCreatableFromDatabase && typeof(T).GetInterfaces().Contains(typeof(ILoadableFromDatabase<T>));
            bool createableFromExistingObjects = IsCreatableFromExistingObjects && ExistingItems.Count > 0;

            if (createableFromScratch && !createableFromFile && !createableFromDatabase && !createableFromExistingObjects) CreateFromScratch();
            else if (!createableFromScratch && createableFromFile && !createableFromDatabase && !createableFromExistingObjects) CreateFromFile();
            else if (!createableFromScratch && !createableFromFile && createableFromDatabase && !createableFromExistingObjects) CreateFromDatabase();
            else if (!createableFromScratch && !createableFromFile && !createableFromDatabase && createableFromExistingObjects) CreateFromExistingItems();
            else
            {
                CreatorWindow creatorWindow = ApplicationState.WindowsManager.Open<CreatorWindow>("Creator window", true);
                creatorWindow.IsCreatableFromScratch = createableFromScratch;
                creatorWindow.IsCreatableFromExistingObjects = createableFromExistingObjects;
                creatorWindow.IsCreatableFromFile = createableFromFile;
                creatorWindow.IsCreatableFromDatabase = createableFromDatabase;
                creatorWindow.OnOk.AddListener(() => OnSaveCreator(creatorWindow));
                WindowsReferencer.Add(creatorWindow);
            }
        }
        public virtual void CreateFromScratch()
        {
            OpenModifier(new T());
        }
        public virtual void CreateFromExistingItems()
        {
            OpenSelector(ExistingItems);
        }
        public virtual void CreateFromFile()
        {
            if (LoadFromFile(out T item))
            {
                OpenModifier(item);
            }
        }
        public virtual void CreateFromDatabase()
        {
            SelectDatabase();
        }
        #endregion

        #region Private Methods
        protected virtual void OnSaveCreator(CreatorWindow creatorWindow)
        {
            switch (creatorWindow.Type)
            {
                case HBP.Data.Enums.CreationType.FromScratch:
                    CreateFromScratch();
                    break;
                case HBP.Data.Enums.CreationType.FromExistingObject:
                    CreateFromExistingItems();
                    break;
                case HBP.Data.Enums.CreationType.FromFile:
                    CreateFromFile();
                    break;
                case HBP.Data.Enums.CreationType.FromDatabase:
                    CreateFromDatabase();
                    break;
            }
        }
        protected virtual void OpenSelector(IEnumerable<T> objects, bool multiSelection = false, bool openModifiers = true, bool generateNewIDs = true)
        {
            ObjectSelector<T> selector = ApplicationState.WindowsManager.OpenSelector<T>(objects, multiSelection, openModifiers);
            selector.OnOk.AddListener(() => OnSaveSelector(selector, generateNewIDs));
            WindowsReferencer.Add(selector);
        }
        protected virtual void OnSaveSelector(ObjectSelector<T> selector, bool generateNewIDs = true)
        {
            foreach (var obj in selector.ObjectsSelected)
            {
                T clone = (T)obj.Clone();
                if (generateNewIDs)
                {
                    if (typeof(T).GetInterfaces().Contains(typeof(IIdentifiable)))
                    {
                        IIdentifiable identifiable = clone as IIdentifiable;
                        identifiable.GenerateID();
                    }
                }
                if (clone != null)
                {
                    if (selector.OpenModifiers)
                    {
                        OpenModifier(clone);
                    }
                    else
                    {
                        OnObjectCreated.Invoke(obj);
                    }
                }
            }
        }

        protected virtual ObjectModifier<T> OpenModifier(T item)
        {
            ObjectModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, true);
            modifier.OnOk.AddListener(() => OnSaveModifier(modifier));
            WindowsReferencer.Add(modifier);
            return modifier;
        }
        protected virtual void OnSaveModifier(ObjectModifier<T> modifier)
        {
            OnObjectCreated.Invoke(modifier.Item);
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
                    if (identifiable.ID == "xxxxxxxxxxxxxxxxxxxxxxxxx")
                    {
                        identifiable.ID = Guid.NewGuid().ToString();
                    }
                }
                return true;
            }
            return false;
        }
        protected virtual void SelectDatabase()
        {
            string path = FileBrowser.GetExistingDirectoryName();
            if (path != null)
            {
                ILoadableFromDatabase<T> loadable = new T() as ILoadableFromDatabase<T>;
                GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                ApplicationState.LoadingManager.Load(loadable.LoadFromDatabase(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text), (result) => OnEndLoadFromDatabase(result.ToArray())), onChangeProgress);
            }
        }
        protected virtual void OnEndLoadFromDatabase(T[] result)
        {
            if (result.Length > 0)
            {
                OpenSelector(result, true, false, false);
            }
        }
        #endregion
    }
}