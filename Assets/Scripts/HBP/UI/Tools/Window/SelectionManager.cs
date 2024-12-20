using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class SelectionManager : Singleton<SelectionManager>
    {
        #region Properties
        [SerializeField] private Selector m_Selection;
        List<Selector> m_Selectors = new List<Selector>();
        public GraphicRaycaster GraphicRaycaster;
        #endregion

        #region Public Methods
        public static void Add(Selector selector)
        {
            m_Instance.m_Selectors.Add(selector);
            selector.OnChangeValue.AddListener((selected) => m_Instance.OnChangeSelection(selected, selector));
            selector.Selected = true;
        }
        public static void Remove(Selector selector)
        {
            m_Instance.m_Selectors.Remove(selector);
        }
        #endregion

        #region Private Methods
        void OnChangeSelection(bool selected, Selector selector)
        {
            if (selected)
            {
                m_Selection = selector;
                foreach (var s in m_Selectors.Where((s) => s != selector))
                {
                    s.Selected = false;
                }
            }
            else
            {
                if (m_Selection == selector) m_Selection = null;
            }
        }
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Selector selector = null;
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                pointerEventData.position = Input.mousePosition;
                var results = FindObjectsOfType<GraphicRaycaster>().SelectMany(r => { List<RaycastResult> res = new List<RaycastResult>(); r.Raycast(pointerEventData, res); return res; }).OrderByDescending(r => r.sortingOrder).ThenByDescending(r => r.depth);
                foreach (var result in results)
                {
                    selector = result.gameObject.GetComponentInParent<Selector>();
                    if (selector != null) break;
                }

                if (selector != null)
                {
                    selector.Selected = true;
                }
                else
                {
                    if (m_Selection != null)
                    {
                        m_Selection.Selected = false;
                        m_Selection = null;
                    }
                }
            }
        }

        static public Rect GetWorldRect(RectTransform rt, Vector2 scale)
        {
            // Convert the rectangle to world corners and grab the top left
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 topLeft = corners[0];

            // Rescale the size appropriately based on the current Canvas scale
            Vector2 scaledSize = new Vector2(scale.x * rt.rect.size.x, scale.y * rt.rect.size.y);

            return new Rect(topLeft, scaledSize);
        }
        #endregion
    }
}