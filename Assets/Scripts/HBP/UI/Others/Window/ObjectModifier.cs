using System;

namespace HBP.UI
{
    public abstract class ObjectModifier<T> : DialogWindow where T : ICloneable , ICopiable
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
        public override void OK()
        {
            UnityEngine.Debug.Log("Ok");
            WindowsReferencer.SaveAll();
            Item.Copy(ItemTemp);
            OnOk.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected abstract void SetFields(T objectToDisplay);
        #endregion
    }
}