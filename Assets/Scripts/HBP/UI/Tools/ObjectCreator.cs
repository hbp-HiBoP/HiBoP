using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Enums;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Tools
{
    /// <summary>
    /// Component to create a new object ICloneable and ICopiable. 
    /// </summary>
    /// <typeparam name="T">Type of the object to create</typeparam>
    [Serializable]
    public class ObjectCreator<T> : MonoBehaviour where T : Core.Data.BaseData, new()
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

        [SerializeField] bool m_IsLoadableFromDirectory = true;
        /// <summary>
        /// True if the Object of type T is creatable from a database, False otherwise.
        /// </summary>
        public bool IsCreatableFromDirectory
        {
            get
            {
                return m_IsLoadableFromDirectory;
            }
            set
            {
                m_IsLoadableFromDirectory = value;
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

        public Func<T, bool> DatabaseFilterMethod { get; set; } = o => true;

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        /// <summary>
        /// Windows references used to manage sub windows opened by the object creator.
        /// </summary>
        public virtual WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }

        [SerializeField] protected CreatorContextMenu m_CreatorContextMenu;

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
            if (m_CreatorContextMenu.gameObject.activeSelf)
            {
                m_CreatorContextMenu.Close();
                return;
            }
            bool createableFromScratch = IsCreatableFromScratch;
            bool createableFromFile = IsCreatableFromFile && typeof(T).GetInterfaces().Contains(typeof(ILoadable<T>));
            bool createableFromDatabase = IsCreatableFromDatabase && typeof(T).GetInterfaces().Contains(typeof(ILoadableFromDatabase<T>));
            bool createableFromExistingObjects = IsCreatableFromExistingObject && ExistingObjects.Count > 0;
            bool creatableFromDirectory = IsCreatableFromDirectory && typeof(T).GetInterfaces().Contains(typeof(ILoadableFromDirectory<T>));

            if (createableFromScratch && !createableFromFile && !createableFromDatabase && !createableFromExistingObjects && !creatableFromDirectory) CreateFromScratch();
            else if (!createableFromScratch && createableFromFile && !createableFromDatabase && !createableFromExistingObjects && !creatableFromDirectory) CreateFromFile();
            else if (!createableFromScratch && !createableFromFile && createableFromDatabase && !createableFromExistingObjects && !creatableFromDirectory) CreateFromDatabase();
            else if (!createableFromScratch && !createableFromFile && !createableFromDatabase && createableFromExistingObjects && !creatableFromDirectory) CreateFromExistingObject();
            else if (!createableFromScratch && !createableFromFile && !createableFromDatabase && !createableFromExistingObjects && creatableFromDirectory) CreateFromDirectory();
            else
            {
                m_CreatorContextMenu.IsCreatableFromScratch = createableFromScratch;
                m_CreatorContextMenu.IsCreatableFromExistingObjects = createableFromExistingObjects;
                m_CreatorContextMenu.IsCreatableFromFile = createableFromFile;
                m_CreatorContextMenu.IsCreatableFromDatabase = createableFromDatabase;
                m_CreatorContextMenu.IsCreatableFromDirectory = creatableFromDirectory;
                m_CreatorContextMenu.Open();
            }
        }
        /// <summary>
        /// Create a new object with a specified creation type.
        /// </summary>
        /// <param name="type">Creation type.</param>
        public virtual void Create(CreationType type)
        {
            switch (type)
            {
                case CreationType.FromScratch:
                    CreateFromScratch();
                    break;
                case CreationType.FromExistingObject:
                    CreateFromExistingObject();
                    break;
                case CreationType.FromFile:
                    CreateFromFile();
                    break;
                case CreationType.FromDatabase:
                    CreateFromDatabase();
                    break;
                case CreationType.FromDirectory:
                    CreateFromDirectory();
                    break;
            }
            m_CreatorContextMenu.Close();
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
            LoadFromDatabase().Forget();
        }
        /// <summary>
        /// Create a new object from a directory
        /// </summary>
        public virtual void CreateFromDirectory()
        {
            LoadFromDirectory().Forget();
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_CreatorContextMenu.OnSelectType.AddListener(Create);
        }
        /// <summary>
        /// Open a new object selector.
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="multiSelection"></param>
        /// <param name="openModifiers"></param>
        /// <param name="generateNewIDs"></param>
        protected virtual void OpenSelector(IEnumerable<T> objects, bool multiSelection = false, bool openModifiers = true, bool generateNewIDs = true)
        {
            ObjectSelector<T> selector = WindowsManager.OpenSelector(objects, GetComponentInParent<Window>(), multiSelection, openModifiers);
            selector.OnOk.AddListener(() => SaveSelector(selector, generateNewIDs).Forget());
            WindowsReferencer.Add(selector);
        }
        protected virtual async UniTaskVoid SaveSelector(ObjectSelector<T> selector, bool generateNewIDs)
        {
            await LoadingManager.LoadAsync(update => SaveSelectorAsync(selector, generateNewIDs, update));
        }
        /// <summary>
        /// Create clone of the objects selected in the ObjectSelector.
        /// </summary>
        /// <param name="selector">Object selector</param> 
        /// <param name="generateNewIDs">True if generate a new ID for every objects cloned, False otherwise.</param>
        protected virtual async UniTask SaveSelectorAsync(ObjectSelector<T> selector, bool generateNewIDs, Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.SwitchToThreadPool();
            var length = selector.ObjectsSelected.Length;
            var progress = 0;
            var cloneList = new List<T>();
            foreach (var obj in selector.ObjectsSelected)
            {
                updateProgress.Invoke((float)progress++ / length, 0, new LoadingText($"Importing {progress}/{length}"));
                T clone = (T)obj.Clone();
                if (clone != null)
                {
                    cloneList.Add(clone);
                }
            }
            if (generateNewIDs && typeof(T).GetInterfaces().Contains(typeof(IIdentifiable)))
            {
                foreach (var clone in cloneList)
                {
                    IIdentifiable identifiable = clone;
                    identifiable.GenerateID();
                }
            }
            await UniTask.SwitchToMainThread();
            foreach (var clone in cloneList)
            {
                if (selector.OpenModifiers)
                {
                    OpenModifier(clone);
                }
                else
                {
                    OnObjectCreated.Invoke(clone);
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
            ObjectModifier<T> modifier = WindowsManager.OpenModifier(@object, GetComponentInParent<Window>());
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
                                items.AddRange(array);
                            }
                        }
                    }
                }
                foreach (var item in items)
                {
                    OnObjectCreated.Invoke(item);
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
                        items.AddRange(array);
                    }
                }
            }
            foreach (var item in items)
            {
                OnObjectCreated.Invoke(item);
            }
#endif
        }
        protected virtual async UniTaskVoid LoadFromDirectory()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingDirectoryNamesAsync(async (paths) =>
            {
                if (paths.Length > 0)
                {
                    ILoadableFromDirectory<T> loadable = new T() as ILoadableFromDirectory<T>;
                    var result = await LoadingManager.LoadAsync(update => loadable.LoadFromDirectory(paths, update));
                    var length = result.Count();
                    if (length > 0)
                    {
                        if (length == 1)
                            OnObjectCreated.Invoke(result.First());
                        else
                            OpenSelector(result, true, false, false);
                    }
                }
            });
#else
            string[] paths = FileBrowser.GetExistingDirectoryNames();
            if (paths.Length > 0)
            {
                ILoadableFromDirectory<T> loadable = new T() as ILoadableFromDirectory<T>;
                var result = await LoadingManager.LoadAsync(update => loadable.LoadFromDirectory(paths, update));
                await UniTask.SwitchToMainThread();
                var length = result.Count();
                if (length > 0)
                {
                    if (length == 1)
                        OnObjectCreated.Invoke(result.First());
                    else
                        OpenSelector(result, true, false, false);
                }
            }
#endif
        }
        protected virtual async UniTaskVoid LoadFromDatabase()
        {
            ILoadableFromDatabase<T> loadable = new T() as ILoadableFromDatabase<T>;
            var result = await LoadingManager.LoadAsync(update => loadable.LoadFromDatabaseAsync(update, DatabaseFilterMethod), false);
            await UniTask.SwitchToMainThread();
            if (result.Count() > 0)
                OpenSelector(result, true, false, false);
        }
        #endregion
    }
}