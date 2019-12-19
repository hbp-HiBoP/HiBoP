using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    public class WindowsManager : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject[] m_Windows;
        public RectTransform Container;
        public WindowsReferencer WindowsReferencer;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Windows = Resources.LoadAll<GameObject>("Prefabs/UI/Windows/");
        }
        public Window Open(string name, bool interactable = true)
        {
            Window window = null;
            GameObject prefab = GetWindowPrefab(name);
            if (prefab)
            {
                window = CreateWindow(prefab, interactable);
            }
            return window;
        }
        public T Open<T>(string name, bool interactable = true) where T : Window
        {
            T window = default;
            GameObject prefab = GetWindowPrefab(name);
            if (prefab)
            {
                window = CreateWindow(prefab, interactable) as T;
            }
            return window;
        }
        public ObjectModifier<T> OpenModifier<T>(T obj, bool interactable = true) where T : ICopiable, ICloneable
        {
            ObjectModifier<T> modifier = default;
            GameObject prefab = GetWindowPrefab(typeof(ObjectModifier<T>));
            if (prefab)
            {
                modifier = CreateWindow(prefab, interactable) as ObjectModifier<T>;
                modifier.Item = obj;
            }
            return modifier;
        }
        public ObjectSelector<T> OpenSelector<T>(IEnumerable<T> objects, bool multiSelection = true, bool openModifiers = false, bool interactable = true)
        {
            ObjectSelector<T> selector = default;
            GameObject prefab = GetWindowPrefab(typeof(ObjectSelector<T>));
            if (prefab)
            {
                selector = CreateWindow(prefab, interactable) as ObjectSelector<T>;
                selector.Objects = objects.ToArray();
                if(multiSelection) selector.Selection = ObjectSelector<T>.SelectionType.Multi;
                else selector.Selection = ObjectSelector<T>.SelectionType.Single;
                selector.OpenModifiers = openModifiers;
            }
            return selector;
        }
        #endregion

        #region Private Methods
        Window CreateWindow(GameObject prefab, bool interactable)
        {
            GameObject gameObject = Instantiate(prefab, Container);
            RectTransform rectTransform = gameObject.transform as RectTransform;
            gameObject.transform.localPosition = (rectTransform.pivot - new Vector2(0.5f, 0.5f)) * rectTransform.rect.size;
            var window = gameObject.GetComponent<Window>();
            window.Interactable = interactable;
            WindowsReferencer.Add(window);
            return window;
        }
        GameObject GetWindowPrefab(string name)
        {
            return m_Windows.First(w => w.name == name);
        }
        GameObject GetWindowPrefab(Type type)
        {
            return m_Windows.FirstOrDefault(g => g.GetComponent(type) != null);
        }
        #endregion
    }
}