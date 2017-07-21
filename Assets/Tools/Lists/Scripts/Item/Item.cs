using UnityEngine;

namespace Tools.Unity.Lists
{
    public abstract class Item<T> : MonoBehaviour
    {     
        #region Properties
        protected T m_Object;
        public virtual T Object
        {
            get { return m_Object; }
            set { m_Object = value;}
        }
        public virtual System.Type Type
        {
            get { return typeof(T); }
        }
        #endregion
    }
}