using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    #region Properties
    static SelectionManager m_Instance;
    public Selector m_Selection;
    List<Selector> m_Selectors = new List<Selector>();
	#endregion

	#region Public Methods
    public void Add(Selector selector)
    {
        m_Selectors.Add(selector);
        selector.OnChangeValue.AddListener((selected) => OnChangeSelection(selected, selector));
        selector.Selected = true;
    }
    public void Remove(Selector selector)
    {
        m_Selectors.Remove(selector);
    }
    #endregion

    #region Private Methods
    void Awake()
    {
        if(m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
        }
        m_Instance = this;
    }
    void OnChangeSelection(bool selected, Selector selector)
    {
        if(selected)
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
        if(Input.GetMouseButtonDown(0))
        {
            Selector selector = null;
            RectTransform selectorRectTransform = null;
            foreach (var item in m_Selectors)
            {
                RectTransform rectTransform = item.GetComponent<RectTransform>();
                if(RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
                {
                    if(selector == null || (selector != null && selectorRectTransform != null && selectorRectTransform.GetSiblingIndex() < rectTransform.GetSiblingIndex()))
                    {
                        selector = item;
                        selectorRectTransform = rectTransform;
                    }
                }
            }
            if(selector != null)
            {
                selector.Selected = true;
            }
            else
            {
                if(m_Selection != null)
                {
                    m_Selection.Selected = false;
                    m_Selection = null;
                }
            }
        }
    }
	#endregion
}