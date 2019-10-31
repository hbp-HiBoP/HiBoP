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

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        public virtual WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }
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
            ObjectCreator.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        protected virtual ObjectModifier<T> OpenModifier(T item, bool interactable)
        {
            ObjectModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnSave.AddListener(() => Add(modifier.Item));
            WindowsReferencer.Add(modifier);
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