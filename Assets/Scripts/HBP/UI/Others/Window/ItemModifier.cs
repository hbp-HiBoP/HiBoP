using System;

namespace HBP.UI
{
    public abstract class ItemModifier<T> : SavableWindow where T : ICloneable , ICopiable
    {
        #region Properties
        protected T item;
        public virtual T Item
        {
            get { return item; }
            set { item = value; ItemTemp = (T)item.Clone(); }
        }

        protected T itemTemp;
        protected virtual T ItemTemp
        {
            get { return itemTemp; }
            set { itemTemp = value; SetFields(itemTemp); }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            SubWindowsManager.SaveAll();
            Item.Copy(ItemTemp);
            OnSave.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected abstract void SetFields(T objectToDisplay);
        #endregion
    }
}