using System;
using UnityEngine.Events;

namespace HBP.UI
{
    public abstract class ItemModifier<T> : SavableWindow where T : ICloneable , ICopiable
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
        #endregion

        #region Public Methods
        public static ItemModifier<T> Open(T objectToModify, bool interactable)
        {
            ItemModifier<T> itemModifier = ItemModifier<T>.Open(interactable) as ItemModifier<T>;
            itemModifier.Item = objectToModify;
            return itemModifier;
        }
        public override void Save()
        {
            Item.Copy(ItemTemp);
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected abstract void SetFields(T objectToDisplay);
        #endregion
    }
}