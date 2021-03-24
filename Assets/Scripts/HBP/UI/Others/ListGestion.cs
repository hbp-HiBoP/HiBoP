using HBP.UI;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Components
{
    /// <summary>
    /// Abstract generic base class which manage a list of elements.
    /// </summary>
    public abstract class ListGestion<T> : MonoBehaviour where T : HBP.Data.BaseData, new()
    {
        #region Properties
        [SerializeField] protected bool m_Interactable;
        /// <summary>
        /// Use to enable or disable the ability to select a selectable UI element (for example, a Button).
        /// </summary>
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
        /// <summary>
        /// Use to enable or disable the ability to modify elements of the list.
        /// </summary>
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

        /// <summary>
        /// UI list which display the elements.
        /// </summary>
        public abstract Lists.ActionableList<T> List { get; }
        /// <summary>
        /// ObjectCreator contains all the tools to create a new element.
        /// </summary>
        public abstract ObjectCreator<T> ObjectCreator { get; }

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        /// <summary>
        /// Children windows referencer.
        /// </summary>
        public virtual WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }

        [SerializeField] Button m_CreateButton, m_RemoveButton;
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a new elements and add it to the list.
        /// </summary>
        public virtual void Create()
        {
            ObjectCreator.ExistingObjects = List.Objects.ToList();
            ObjectCreator.Create();
        }
        /// <summary>
        /// Remove all the selected elements to the list.
        /// </summary>
        public virtual void RemoveSelected()
        {
            List.Remove(List.ObjectsSelected);
        }
        #endregion

        #region Protected Methods
        void OnValidate()
        {
            Interactable = Interactable;
            Modifiable = Modifiable;
        }
        void Awake()
        {
            Initialize();
        }
        /// <summary>
        /// Called on Awake(). You can override this function and use this to initialize anything needed by your list gestion.
        /// </summary>
        protected virtual void Initialize()
        {
            List.OnAction.AddListener((item, v) => OpenModifier(item));
            List.OnAddObject.AddListener(obj => ObjectCreator.ExistingObjects.Add(obj));
            List.OnRemoveObject.AddListener(obj => ObjectCreator.ExistingObjects.Remove(obj));
            List.OnUpdateObject.AddListener(OnUpdateObject);
            ObjectCreator.ExistingObjects = List.Objects.ToList();
            ObjectCreator.OnObjectCreated.AddListener(OnObjectCreated);
            ObjectCreator.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        /// <summary>
        /// Open a ObjectModifier to modify a object.
        /// </summary>
        /// <param name="obj">Object to modify</param>
        /// <returns>ObjectModifier</returns>
        protected virtual ObjectModifier<T> OpenModifier(T obj)
        {
            ObjectModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(obj, m_Modifiable);
            modifier.OnOk.AddListener(() => OnSaveModifier(modifier.Object));
            WindowsReferencer.Add(modifier);
            return modifier;
        }
        /// <summary>
        /// Callback executed when a ObjectModifier is modified.
        /// </summary>
        /// <param name="obj">Object modified</param>
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
        /// <summary>
        /// Callback executed when a object is created.
        /// </summary>
        /// <param name="obj">Object created</param>
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
        /// <summary>
        /// Callback executed when a object is update.
        /// </summary>
        /// <param name="obj">Object to update</param>
        protected virtual void OnUpdateObject(T obj)
        {
            int index = ObjectCreator.ExistingObjects.FindIndex(o => o.Equals(obj));
            ObjectCreator.ExistingObjects[index] = obj;
        }
        #endregion
    }
}