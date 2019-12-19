using HBP.UI;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
                m_CreateButton.interactable = value;
                m_RemoveButton.interactable = value;
            }
        }

        [SerializeField] protected bool m_Modifiable;
        public bool Modifiable
        {
            get
            {
                return m_Modifiable;
            }
            set
            {
                m_Modifiable = value;
                foreach (var modifier in m_WindowsReferencer.Windows.OfType<ObjectModifier<T>>())
                {
                    modifier.Interactable = value;
                }
            }
        }

        public abstract Lists.ActionableList<T> List { get; }
        public abstract ObjectCreator<T> ObjectCreator { get; }

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        public virtual WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }

        [SerializeField] Button m_CreateButton, m_RemoveButton;
        #endregion

        #region Public Methods
        public virtual void Create()
        {
            ObjectCreator.ExistingItems = List.Objects.ToList();
            ObjectCreator.Create();
        }
        public virtual void RemoveSelected()
        {
            List.Remove(List.ObjectsSelected);
        }
        #endregion

        #region Protected Methods
        private void OnValidate()
        {
            Interactable = Interactable;
            Modifiable = Modifiable;
        }
        void Awake()
        {
            Initialize();
        }
        protected virtual void Initialize()
        {
            List.OnAction.AddListener((item, v) => OpenModifier(item));
            List.OnAddObject.AddListener(obj => ObjectCreator.ExistingItems.Add(obj));
            List.OnRemoveObject.AddListener(obj => ObjectCreator.ExistingItems.Remove(obj));
            List.OnUpdateObject.AddListener(OnUpdateObject);
            ObjectCreator.ExistingItems = List.Objects.ToList();
            ObjectCreator.OnObjectCreated.AddListener(OnObjectCreated);
            ObjectCreator.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        protected virtual ObjectModifier<T> OpenModifier(T item)
        {
            ObjectModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, m_Modifiable);
            modifier.OnOk.AddListener(() => OnSaveModifier(modifier.Item));
            WindowsReferencer.Add(modifier);
            return modifier;
        }
        protected virtual void OnSaveModifier(T obj)
        {
            if(obj is INameable nameable)
            {
                if (List.Objects.Any(c => (c as INameable).Name == nameable.Name && !c.Equals(obj)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", nameable.Name, count);
                    while (List.Objects.OfType<INameable>().Any(c => c.Name == name))
                    {
                        count++;
                        name = string.Format("{0}({1})", nameable.Name, count);
                    }
                    nameable.Name = name;
                }
            }
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }
        protected virtual void OnObjectCreated(T obj)
        {
            if (obj is INameable nameable)
            {
                if (List.Objects.Any(c => (c as INameable).Name == nameable.Name && !c.Equals(obj)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", nameable.Name, count);
                    while (List.Objects.OfType<INameable>().Any(c => c.Name == name))
                    {
                        count++;
                        name = string.Format("{0}({1})", nameable.Name, count);
                    }
                    nameable.Name = name;
                }
            }
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }
        protected virtual void OnUpdateObject(T obj)
        {
            int index = ObjectCreator.ExistingItems.FindIndex(o => o.Equals(obj));
            ObjectCreator.ExistingItems[index] = obj;
        }
        #endregion
    }
}