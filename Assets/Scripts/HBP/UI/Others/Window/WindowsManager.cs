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
        public Dictionary<Type, Vector2> SizeDeltaByWindow = new Dictionary<Type, Vector2>();
        public Vector2 Offset;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Windows = Resources.LoadAll<GameObject>("Prefabs/UI/Windows/");
            WindowsReferencer.OnCloseWindow.AddListener(OnCloseWindow);
        }
        public Window Open(string name, bool interactable = true)
        {
            Window window = WindowsReferencer.Windows.FirstOrDefault(w => w.name == name);
            if (window)
            {
                Selector selector = window.GetComponent<Selector>();
                if (selector)
                {
                    selector.Selected = true;
                }
            }
            else
            {
                GameObject prefab = GetWindowPrefab(name);
                if (prefab)
                {
                    window = CreateWindow(prefab, interactable);
                }
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
        public ObjectModifier<T> OpenModifier<T>(T obj, bool interactable = true) where T : Data.BaseData
        {
            ObjectModifier<T> modifier = WindowsReferencer.Windows.OfType<ObjectModifier<T>>().FirstOrDefault(w => w.Object.ID == obj.ID);
            if (modifier)
            {
                Selector selector = modifier.GetComponent<Selector>();
                if (selector)
                {
                    selector.Selected = true;
                }
            }
            else
            {
                GameObject prefab = GetWindowPrefab(typeof(ObjectModifier<T>));
                if (prefab)
                {
                    modifier = CreateWindow(prefab, interactable) as ObjectModifier<T>;
                    modifier.Object = obj;
                }
            }
            return modifier;
        }
        public ObjectSelector<T> OpenSelector<T>(IEnumerable<T> objects, bool multiSelection = true, bool openModifiers = false, bool interactable = true)
        {
            var openedSelector = WindowsReferencer.Windows.OfType<ObjectSelector<T>>().ToArray();
            foreach (var sel in openedSelector)
            {
                sel.Close();
            }
            ObjectSelector<T> selector = default;
            GameObject prefab = GetWindowPrefab(typeof(ObjectSelector<T>));
            if (prefab)
            {
                selector = CreateWindow(prefab, interactable) as ObjectSelector<T>;
                selector.Objects = objects.ToArray();
                if (multiSelection) selector.Selection = ObjectSelector<T>.SelectionType.Multi;
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
            gameObject.name = prefab.name;
            RectTransform rectTransform = gameObject.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            var window = gameObject.GetComponent<Window>();
            Window existingWindow = WindowsReferencer.Windows.FirstOrDefault(w => w.GetType() == window.GetType());
            if (existingWindow != null)
            {
                rectTransform.sizeDelta = existingWindow.GetComponent<RectTransform>().sizeDelta;
            }
            else
            {
                if(SizeDeltaByWindow.TryGetValue(window.GetType(), out Vector2 sizeDelta))
                {
                    rectTransform.sizeDelta = sizeDelta;
                }
            }

            Window selectedWindow = WindowsReferencer.Windows.FirstOrDefault(w => w.GetComponent<Selector>().Selected);
            if (selectedWindow != null)
            {
                if (selectedWindow is CreatorWindow) rectTransform.anchoredPosition = selectedWindow.GetComponent<RectTransform>().anchoredPosition;
                else rectTransform.anchoredPosition = selectedWindow.GetComponent<RectTransform>().anchoredPosition + Offset;
            }
            else
            {
                rectTransform.anchoredPosition = (rectTransform.pivot - new Vector2(0.5f, 0.5f)) * rectTransform.sizeDelta;
            }
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

        void OnCloseWindow(Window window)
        {
            SizeDeltaByWindow[window.GetType()] = window.GetComponent<RectTransform>().sizeDelta;
        }
        #endregion
    }
}