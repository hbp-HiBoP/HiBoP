using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System;

namespace HBP.UI
{
    public abstract class Gestion<T,P> : MonoBehaviour where T: ICopiable, ICloneable, new()
    {
        #region Properties
        protected abstract SelectableListWithItemAction<T> List {  get; }
        System.Collections.Generic.List<ItemModifier<T>> m_Modifiers = new System.Collections.Generic.List<ItemModifier<T>>();
        [SerializeField] Text m_Counter;
        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;
        protected P m_ParentObject;
        bool m_Interactable;
        public virtual bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                m_AddButton.interactable = interactable;
                m_RemoveButton.interactable = interactable;
                List.Interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public virtual void Set(P patientObject)
        {
            m_ParentObject = patientObject;
            Initialize();
        }
        public virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public virtual void AddItem()
        {
            OpenModifier(new T(), interactable);
        }
        public virtual void RemoveItem()
        {
            List.Remove(List.ObjectsSelected);
            m_Counter.text = List.ObjectsSelected.Count().ToString();
        }
        public abstract void Save();
        public void Close()
        {
            foreach (var modifier in m_Modifiers.ToArray()) modifier.Close();
            m_Modifiers.Clear();
        }
        public virtual void Initialize()
        {
            List.Initialize();
            List.OnSelectionChanged.RemoveAllListeners();
            List.OnSelectionChanged.AddListener(() => m_Counter.text = List.NumberOfItemSelected.ToString());

            List.OnAction.RemoveAllListeners();
            List.OnAction.AddListener((item, i) => OpenModifier(item, interactable));
        }
        #endregion

        #region Private Methods
        protected void OpenModifier(T item, bool interactable)
        {
            ItemModifier<T> modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnClose.AddListener(() => OnCloseModifier(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            m_Modifiers.Add(modifier);
        }
        void OnCloseModifier(ItemModifier<T> modifier)
        {
            m_Modifiers.Remove(modifier);
        }
        protected virtual void OnSaveModifier(ItemModifier<T> modifier)
        {
            if (!List.Objects.Contains(modifier.Item))
            {
                List.Add(modifier.Item);
            }
            else
            {
                List.UpdateObject(modifier.Item);
            }
        }
        #endregion
    }
}