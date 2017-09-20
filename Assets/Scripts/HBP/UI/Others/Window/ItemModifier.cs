using System;
using UnityEngine.Events;

namespace HBP.UI
{
    public abstract class ItemModifier<T> : Window where T : ICloneable , ICopiable
    {
        #region Properties
        protected T item;
        public T Item
        {
            get { return item; }
            protected set { item = value; ItemTemp = (T)item.Clone(); }
        }

        protected T itemTemp;
        protected T ItemTemp
        {
            get { return itemTemp; }
            set { itemTemp = value; SetFields(itemTemp); }
        }

        protected UnityEvent saveEvent = new UnityEvent { };
        public UnityEvent SaveEvent
        {
            get { return saveEvent; }
        }
        #endregion

        #region Public Methods
        public virtual void Open(T objectToModify,bool interactable)
        {
            base.Open();
            Item = objectToModify;
            SetInteractableFields(interactable);
        }
        public virtual void Save()
        {
            Item.Copy(ItemTemp);
            SaveEvent.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected abstract void SetFields(T objectToDisplay);
        protected abstract void SetInteractableFields(bool interactable);
        #endregion
    }
}