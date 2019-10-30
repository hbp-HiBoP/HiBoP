using HBP.UI;
using System;
using System.Linq;
using UnityEngine;

namespace Tools.Unity.Components
{
    public abstract class ListGestion<T> : MonoBehaviour where T : ICloneable, ICopiable, new()
    {
        #region Properties
        [SerializeField] protected bool m_Interactable;
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

        public abstract Lists.SelectableListWithItemAction<T> List { get; }
        public abstract ObjectCreator<T> ObjectCreator { get; }

        [SerializeField] protected SubWindowsManager m_SubWindowsManager = new SubWindowsManager();
        public virtual SubWindowsManager SubWindowsManager { get => m_SubWindowsManager; }
        #endregion

        #region Public Methods
        public virtual void RemoveSelected()
        {
            List.Remove(List.ObjectsSelected);
        }
        #endregion

        #region Protected Methods
        void Awake()
        {
            Initialize();
        }
        protected virtual void Initialize()
        {
            List.OnAction.AddListener((item, v) => OpenModifier(item, Interactable));
            List.OnAddObject.AddListener(ObjectCreator.ExistingItems.Add);
            List.OnRemoveObject.AddListener(obj => ObjectCreator.ExistingItems.Remove(obj));
            ObjectCreator.ExistingItems = List.Objects.ToList();
            ObjectCreator.OnObjectCreated.AddListener(Add);
            ObjectCreator.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
        }
        protected virtual ItemModifier<T> OpenModifier(T item, bool interactable)
        {
            ItemModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnSave.AddListener(() => Add(modifier.Item));
            SubWindowsManager.Add(modifier);
            return modifier;
        }
        protected virtual void Add(T obj)
        {
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }
        #endregion
    }
}