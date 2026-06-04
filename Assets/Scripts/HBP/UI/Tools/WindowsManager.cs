using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Tools
{
    public class WindowsManager : Manager<WindowsManager>
    {
        #region Properties
        [SerializeField] GameObject[] m_Windows;
        [SerializeField] private RectTransform m_ParentContainer;
        private List<RectTransform> m_Containers = new();
        [SerializeField] private GameObject m_ContainerPrefab;
        public static WindowsReferencer WindowsReferencer = new();
        public Dictionary<Type, Vector2> SizeDeltaByWindow = new();
        public Vector2 Offset;
        #endregion

        #region Public Methods
        public static Window Open(string name, Window parent, bool interactable = true)
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
                GameObject prefab = m_Instance.GetWindowPrefab(name);
                if (prefab)
                {
                    window = m_Instance.CreateWindow(prefab, parent, interactable);
                }
            }
            return window;
        }
        public static T Open<T>(string name, Window parent, bool interactable = true) where T : Window
        {
            T window = default;
            GameObject prefab = m_Instance.GetWindowPrefab(name);
            if (prefab)
            {
                window = m_Instance.CreateWindow(prefab, parent, interactable) as T;
            }
            return window;
        }
        public static ObjectModifier<T> OpenModifier<T>(T obj, Window parent, bool interactable = true) where T : Core.Data.BaseData
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
                GameObject prefab = m_Instance.GetWindowPrefab(typeof(ObjectModifier<T>));
                if (prefab)
                {
                    modifier = m_Instance.CreateWindow(prefab, parent, interactable) as ObjectModifier<T>;
                    modifier.Object = obj;
                }
            }
            return modifier;
        }
        public static ObjectSelector<T> OpenSelector<T>(IEnumerable<T> objects, Window parent, bool multiSelection = true, bool openModifiers = false, bool interactable = true)
        {
            var openedSelector = WindowsReferencer.Windows.OfType<ObjectSelector<T>>().ToArray();
            foreach (var sel in openedSelector)
            {
                sel.Close();
            }
            ObjectSelector<T> selector = default;
            GameObject prefab = m_Instance.GetWindowPrefab(typeof(ObjectSelector<T>));
            if (prefab)
            {
                selector = m_Instance.CreateWindow(prefab, parent, interactable) as ObjectSelector<T>;
                selector.Objects = objects.ToArray();
                if (multiSelection) selector.Selection = ObjectSelector<T>.SelectionType.Multi;
                else selector.Selection = ObjectSelector<T>.SelectionType.Single;
                selector.OpenModifiers = openModifiers;
            }
            return selector;
        }
        public static void CloseAll()
        {
            var windows = WindowsReferencer.Windows.ToArray();
            foreach (var window in windows)
            {
                window.Close();
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_Windows = Resources.LoadAll<GameObject>("Prefabs/UI/Windows/");
            WindowsReferencer.OnCloseWindow.AddListener(OnCloseWindow);
        }
        Window CreateWindow(GameObject prefab, Window parent, bool interactable)
        {
            GameObject gameObject = Instantiate(prefab, m_ParentContainer);
            gameObject.name = prefab.name;
            RectTransform rectTransform = gameObject.transform as RectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            var window = gameObject.GetComponent<Window>();
            window.ParentWindow = parent;
            if (m_Instance.m_Containers.Count <= window.Height)
            {
                for (int i = m_Instance.m_Containers.Count; i <= window.Height; i++)
                {
                    m_Instance.m_Containers.Add(Instantiate(m_ContainerPrefab, m_ParentContainer).GetComponent<RectTransform>());
                    m_Instance.m_Containers[i].name = i.ToString();
                }
            }
            window.transform.SetParent(m_Instance.m_Containers[window.Height]);

            Window existingWindow = WindowsReferencer.Windows.FirstOrDefault(w => w.GetType() == window.GetType());
            if (existingWindow != null)
            {
                rectTransform.sizeDelta = existingWindow.GetComponent<RectTransform>().sizeDelta;
            }
            else
            {
                if (SizeDeltaByWindow.TryGetValue(window.GetType(), out Vector2 sizeDelta))
                {
                    rectTransform.sizeDelta = sizeDelta;
                }
            }

            if (parent != null)
            {
                rectTransform.anchoredPosition = parent.GetComponent<RectTransform>().anchoredPosition + Offset;
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