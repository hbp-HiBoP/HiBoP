using HBP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    /// <summary>
    /// Component to create a new object ICloneable and ICopiable. 
    /// </summary>
    /// <typeparam name="T">Type of the object to create</typeparam>
    [Serializable]
    public class ObjectCreator<T> : MonoBehaviour where T : ICloneable, ICopiable, new()
    {
        #region Properties
        [SerializeField] public bool m_IsLoadableFromFile = true;
        /// <summary>
        /// True if the Object of type T is creatable from a file, False otherwise.
        /// </summary>
        public bool IsCreatableFromFile
        {
            get
            {
                return m_IsLoadableFromFile;
            }
            set
            {
                m_IsLoadableFromFile = value;
            }
        }

        [SerializeField] bool m_IsLoadableFromDatabase = true;
        /// <summary>
        /// True if the Object of type T is creatable from a database, False otherwise.
        /// </summary>
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
        /// <summary>
        /// True if the Object of type T is creatable from scratch, False otherwise.
        /// </summary>
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

        [SerializeField] bool m_IsCreatableFromExistingObject = true;
        /// <summary>
        /// True if the Object of type T is creatable from a existing object of type T, False otherwise.
        /// </summary>
        public bool IsCreatableFromExistingObject
        {
            get
            {
                return m_IsCreatableFromExistingObject;
            }
            set
            {
                m_IsCreatableFromExistingObject = value;
            }
        }

        [SerializeField] List<T> m_ExistingObjects = new List<T>();
        /// <summary>
        /// Existing objects to create a new object if is creatable from existing objects.
        /// </summary>
        public List<T> ExistingObjects
        {
            get
            {
                return m_ExistingObjects;
            }
            set
            {
                m_ExistingObjects = value;
            }
        }

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        /// <summary>
        /// Windows references used to manage sub windows opened by the object creator.
        /// </summary>
        public virtual WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }

        /// <summary>
        /// Event raised when a new object is created.
        /// </summary>
        public UnityEvent<T> OnObjectCreated { get; protected set; } = new GenericEvent<T>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a new object. Use a creator window to select the creation type if needed.
        /// </summary>
        public virtual void Create()
        {
            bool createableFromScratch = IsCreatableFromScratch;
            bool createableFromFile = IsCreatableFromFile && typeof(T).GetInterfaces().Contains(typeof(ILoadable<T>));
            bool createableFromDatabase = IsCreatableFromDatabase && typeof(T).GetInterfaces().Contains(typeof(ILoadableFromDatabase<T>));
            bool createableFromExistingObjects = IsCreatableFromExistingObject && ExistingObjects.Count > 0;

            if (createableFromScratch && !createableFromFile && !createableFromDatabase && !createableFromExistingObjects) CreateFromScratch();
            else if (!createableFromScratch && createableFromFile && !createableFromDatabase && !createableFromExistingObjects) CreateFromFile();
            else if (!createableFromScratch && !createableFromFile && createableFromDatabase && !createableFromExistingObjects) CreateFromDatabase();
            else if (!createableFromScratch && !createableFromFile && !createableFromDatabase && createableFromExistingObjects) CreateFromExistingObject();
            else
            {
                CreatorWindow creatorWindow = ApplicationState.WindowsManager.Open<CreatorWindow>("Creator window", true);
                creatorWindow.IsCreatableFromScratch = createableFromScratch;
                creatorWindow.IsCreatableFromExistingObjects = createableFromExistingObjects;
                creatorWindow.IsCreatableFromFile = createableFromFile;
                creatorWindow.IsCreatableFromDatabase = createableFromDatabase;
                creatorWindow.OnOk.AddListener(() => Create(creatorWindow.Type));
                WindowsReferencer.Add(creatorWindow);
            }
        }
        /// <summary>
        /// Create a new object with a specified creation type.
        /// </summary>
        /// <param name="type">Creation type.</param>
        public virtual void Create(HBP.Data.Enums.CreationType type)
        {
            switch (type)
            {
                case HBP.Data.Enums.CreationType.FromScratch:
                    CreateFromScratch();
                    break;
                case HBP.Data.Enums.CreationType.FromExistingObject:
                    CreateFromExistingObject();
                    break;
                case HBP.Data.Enums.CreationType.FromFile:
                    CreateFromFile();
                    break;
                case HBP.Data.Enums.CreationType.FromDatabase:
                    CreateFromDatabase();
                    break;
            }
        }
        /// <summary>
        /// Create a new object from scratch.
        /// </summary>
        public virtual void CreateFromScratch()
        {
            OpenModifier(new T());
        }
        /// <summary>
        /// Create a new object from a existing object.
        /// </summary>
        public virtual void CreateFromExistingObject()
        {
            OpenSelector(ExistingObjects);
        }
        /// <summary>
        /// Create a new object from file.
        /// </summary>
        public virtual void CreateFromFile()
        {
            LoadFromFile();
        }
        /// <summary>
        /// Create a new object from a database.
        /// </summary>
        public virtual void CreateFromDatabase()
        {
            SelectDatabase();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Open a new object selector.
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="multiSelection"></param>
        /// <param name="openModifiers"></param>
        /// <param name="generateNewIDs"></param>
        protected virtual void OpenSelector(IEnumerable<T> objects, bool multiSelection = false, bool openModifiers = true, bool generateNewIDs = true)
        {
            ObjectSelector<T> selector = ApplicationState.WindowsManager.OpenSelector<T>(objects, multiSelection, openModifiers);
            selector.OnOk.AddListener(() => SaveSelector(selector, generateNewIDs));
            WindowsReferencer.Add(selector);
        }
        /// <summary>
        /// Create clone of the objects selected in the ObjectSelector.
        /// </summary>
        /// <param name="selector">Object selector</param> 
        /// <param name="generateNewIDs">True if generate a new ID for every objects cloned, False otherwise.</param>
        protected virtual void SaveSelector(ObjectSelector<T> selector, bool generateNewIDs = true)
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

        /// <summary>
        /// Open a new objectModifier.
        /// </summary>
        /// <param name="object">Object to modify</param>
        /// <returns>Return the objectModifier.</returns>
        protected virtual ObjectModifier<T> OpenModifier(T @object)
        {
            ObjectModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(@object, true);
            modifier.OnOk.AddListener(() => SaveModifier(modifier));
            WindowsReferencer.Add(modifier);
            return modifier;
        }
        /// <summary>
        /// Save Object modifier.
        /// </summary>
        /// <param name="modifier">Object modifier</param>
        protected virtual void SaveModifier(ObjectModifier<T> modifier)
        {
            OnObjectCreated.Invoke(modifier.Object);
        }

        /// <summary>
        /// Load objects from a file.
        /// </summary>
        /// <param name="result">Objects loaded from the file.</param>
        /// <returns>True if the method end without errors, False otherwise.</returns>
        protected virtual void LoadFromFile()
        {
            List<T> items = new List<T>();
            ILoadable<T> loadable = new T() as ILoadable<T>;
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingFileNamesAsync((paths) =>
            {
                foreach (var rawPath in paths)
                {
                    if (rawPath != null)
                    {
                        string path = rawPath.StandardizeToPath();
                        if (path != string.Empty)
                        {
                            bool loadResult = loadable.LoadFromFile(path, out T[] array);
                            if (loadResult)
                            {
                                if (typeof(T).GetInterfaces().Contains(typeof(IIdentifiable)))
                                {
                                    foreach (T t in array)
                                    {
                                        IIdentifiable identifiable = t as IIdentifiable;
                                        if (identifiable.ID == "xxxxxxxxxxxxxxxxxxxxxxxxx")
                                        {
                                            identifiable.ID = Guid.NewGuid().ToString();
                                        }
                                    }
                                }
                                items.AddRange(array);
                            }
                        }
                    }
                }
                if (items.Count > 0)
                {
                    if (items.Count == 1)
                    {
                        OpenModifier(items[0]);
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            OnObjectCreated.Invoke(item);
                        }
                    }
                }
            }, loadable.GetExtensions());
#else
            string[] paths = FileBrowser.GetExistingFileNames(loadable.GetExtensions());
            foreach (var rawPath in paths)
            {
                string path = rawPath.StandardizeToPath();
                if (path != string.Empty)
                {
                    bool loadResult = loadable.LoadFromFile(path, out T[] array);
                    if (loadResult)
                    {
                        if (typeof(T).GetInterfaces().Contains(typeof(IIdentifiable)))
                        {
                            foreach (T t in array)
                            {
                                IIdentifiable identifiable = t as IIdentifiable;
                                if (identifiable.ID == "xxxxxxxxxxxxxxxxxxxxxxxxx")
                                {
                                    identifiable.ID = Guid.NewGuid().ToString();
                                }
                            }
                        }
                        items.AddRange(array);
                    }
                }
            }
            if (items.Count > 0)
            {
                if (items.Count == 1)
                {
                    OpenModifier(items[0]);
                }
                else
                {
                    foreach (var item in items)
                    {
                        OnObjectCreated.Invoke(item);
                    }
                }
            }
#endif
        }
        /// <summary>
        /// Open a browser to select a folder database and load objects asynchroniously.
        /// </summary>
        protected virtual void SelectDatabase()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingDirectoryNameAsync((path) =>
            {
                if (path != null)
                {
                    ILoadableFromDatabase<T> loadable = new T() as ILoadableFromDatabase<T>;
                    GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                    ApplicationState.LoadingManager.Load(loadable.LoadFromDatabase(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text), (result) => OnEndLoadFromDatabase(result.ToArray())), onChangeProgress);
                }
            });
#else
            string path = FileBrowser.GetExistingDirectoryName();
            if (path != null)
            {
                ILoadableFromDatabase<T> loadable = new T() as ILoadableFromDatabase<T>;
                GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                ApplicationState.LoadingManager.Load(loadable.LoadFromDatabase(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text), (result) => OnEndLoadFromDatabase(result.ToArray())), onChangeProgress);
            }
#endif
        }
        /// <summary>
        /// Called when the asynchronious method to load objects from the database are ended.
        /// </summary>
        /// <param name="result">Objects created from the database</param>
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