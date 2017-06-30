using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CielaSpike;

namespace Tools.Unity.Lists
{
    public abstract class ActivableList<T> : SelectableList<T>
    {
        #region Public Methods
        public virtual void ActivateAllObjects()
        {
            ActiveObject(m_Objects.ToArray());
        }
        public virtual void DeactivateAllOjects()
        {
            DeactivateObject(m_Objects.ToArray());
        }
        public virtual void ActiveObject(T objectToActive)
        {
            StartCoroutine(SetObjectActive(objectToActive, true));
        }
        public virtual void ActiveObject(T[] objectsToActive)
        {
            foreach (T obj in objectsToActive)
            {
                ActiveObject(obj);
            }
        }
        public virtual void DeactivateObject(T objectToDeactivate)
        {
            StartCoroutine(SetObjectActive(objectToDeactivate,false));
        }
        public virtual void DeactivateObject(T[] objectsToDeactivate)
        {
            foreach (T obj in objectsToDeactivate)
            {
                DeactivateObject(obj);
            }
        }
        public virtual void Display(T[] objectsToDisplay, T[] objectsDeactivated)
        {
            this.StartCoroutine(c_Display(objectsToDisplay, objectsDeactivated, true));
        }

        public virtual void Add(T objectToAdd, bool active)
        {
            Add(objectToAdd);
            (Get(objectToAdd) as SelectableItem<T>).Interactable = active;
        }
        #endregion

        #region Protected Methods
        protected virtual IEnumerator c_Display(T[] objectsToDisplay,T[] objectsDeactivated, bool update)
        {
            m_IsWaiting = true;
            while (m_IsDisplaying)
            {
                yield return null;
            }
            m_IsWaiting = false;
            m_IsDisplaying = true;
            if (objectsToDisplay.Length == 0)
            {
                yield return Ninja.JumpToUnity;
                Clear();
                yield return Ninja.JumpBack;
            }
            else
            {
                List<T> m_objToAdd = new List<T>();
                List<T> m_objToRemove = new List<T>();
                List<T> m_objToUpdate = new List<T>();

                // Find obj to remove.
                foreach (T obj in m_Objects)
                {
                    if (!objectsToDisplay.Contains(obj))
                    {
                        m_objToRemove.Add(obj);
                    }
                }

                // Find obj to add.
                foreach (T obj in objectsToDisplay)
                {
                    if (!m_Objects.Contains(obj))
                    {
                        m_objToAdd.Add(obj);
                    }
                    else
                    {
                        m_objToUpdate.Add(obj);
                    }
                }

                // Remove obj.
                foreach (T obj in m_objToRemove)
                {
                    yield return Ninja.JumpToUnity;
                    Remove(obj);
                    yield return Ninja.JumpBack;
                }

                // Add obj.
                foreach (T obj in m_objToAdd)
                {
                    yield return Ninja.JumpToUnity;
                    if(objectsDeactivated.Contains(obj))
                    {
                        Add(obj,false);
                    }
                    else
                    {
                        Add(obj, true);
                    }
                    yield return Ninja.JumpBack;
                }

                if (update)
                {
                    // Update obj
                    foreach (T obj in m_objToUpdate)
                    {
                        yield return Ninja.JumpToUnity;
                        UpdateObj(obj);
                        if (objectsDeactivated.Contains(obj))
                        {
                            (Get(obj) as SelectableItem<T>).Interactable = false;
                        }
                        yield return Ninja.JumpBack;
                    }
                }
            }
            m_IsDisplaying = false;
        }

        protected virtual IEnumerator SetObjectActive(T objectToSet, bool active)
        {
            while (m_IsDisplaying || m_IsWaiting)
            {
                yield return null;
            }
            (Get(objectToSet) as SelectableItem<T>).Interactable = active;
        }
        #endregion
    }
}
