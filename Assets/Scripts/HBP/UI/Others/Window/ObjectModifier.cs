using System;

namespace HBP.UI
{
    /// <summary>
    /// Abstract window to modify a object.
    /// </summary>
    /// <typeparam name="T">Type of the object to modify</typeparam>
    public abstract class ObjectModifier<T> : DialogWindow where T : Data.BaseData
    {
        #region Properties
        protected T m_Object;
        /// <summary>
        /// Object to modify.
        /// </summary>
        public virtual T Object
        {
            get { return m_Object; }
            set { m_Object = value; ObjectTemp = (T)m_Object.Clone(); }
        }

        protected T m_ObjectTemp;
        /// <summary>
        /// Temporary object modified.
        /// </summary>
        protected virtual T ObjectTemp
        {
            get { return m_ObjectTemp; }
            set { m_ObjectTemp = value; SetFields(m_ObjectTemp); }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            WindowsReferencer.SaveAll();
            Object.Copy(ObjectTemp);
            OnOk.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToModify">object to display</param>
        protected abstract void SetFields(T objectToModify);
        #endregion
    }
}