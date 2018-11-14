using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI
{
    public class WindowsManager : MonoBehaviour
    {
        #region Properties
        public RectTransform Container;
        public PrefabReferencer Referencer;
        public List<Window> Windows;
        #endregion

        #region Private Methods
        public Window Open(string name, bool interactable = true)
        {
            Window window = null;
            GameObject prefab = Referencer.GetPrefab(name);
            if(prefab)
            {
                GameObject go = Instantiate(prefab, Container);
                go.transform.localPosition = Vector3.zero;

                // SetWindow
                window = go.GetComponent<Window>();
                window.Interactable = interactable;
            }
            OnOpen(window);
            return window;
        }
        public T Open<T>(string name, bool interactable = true) where T : Window
        {
            T window = default(T);
            GameObject prefab = Referencer.GetPrefab(name);
            if (prefab)
            {
                GameObject go = Instantiate(prefab, Container);
                go.transform.localPosition = Vector3.zero;

                // SetWindow
                window = (T) go.GetComponent<Window>();
                window.Interactable = interactable;
            }
            OnOpen(window);
            return window;
        }
        public ItemModifier<T> OpenModifier<T>(T itemToModify, bool interactable) where T : ICopiable, ICloneable
        {
            ItemModifier<T> modifier = default(ItemModifier<T>);
            GameObject prefab = Referencer.GetPrefab(typeof(ItemModifier<T>));
            if (prefab)
            {
                GameObject go = Instantiate(prefab, Container);
                go.transform.localPosition = Vector3.zero;

                modifier = go.GetComponent<ItemModifier<T>>();
                modifier.Item = itemToModify;
                modifier.Interactable = interactable;
            }
            OnOpen(modifier);
            return modifier;
        }

        public ObjectSelector<T> OpenSelector<T>()
        {
            ObjectSelector<T> selector = default(ObjectSelector<T>);
            GameObject prefab = Referencer.GetPrefab(typeof(ObjectSelector<T>));
            if (prefab)
            {
                GameObject go = Instantiate(prefab, Container);
                go.transform.localPosition = Vector3.zero;

                selector = go.GetComponent<ObjectSelector<T>>();
                //selector.Item = itemToModify;
                //selector.Interactable = interactable;
            }
            OnOpen(selector);
            return selector;
        }
        #endregion

        #region Private Methods
        void OnOpen(Window window)
        {
            if(window != null)
            {
                Windows.Add(window);
                window.OnClose.AddListener(() => OnClose(window));
            }
        }
        void OnClose(Window window)
        {
            Windows.Remove(window);
        }
        #endregion
    }
}