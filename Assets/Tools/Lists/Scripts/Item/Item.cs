using UnityEngine;

namespace Tools.Unity.Lists
{
    public abstract class Item<T> : MonoBehaviour
    {     
        #region Properties
        protected T m_Object;
        public T Object
        {
            get { return m_Object; }
            set { m_Object = value; SetObject(value); }
        }
        #endregion

        #region Public Methods
        public virtual System.Type GetObjectType()
        {
            return typeof(T);
        }
        #endregion

        #region Abstract Methods
        protected abstract void SetObject(T objectToSet);
        
        #endregion
    }
}