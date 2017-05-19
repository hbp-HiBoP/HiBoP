using HBP.Module3D;
using Tools.Unity.ResizableGrid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class View3DUI : MonoBehaviour {
    #region Properties
    private View3D m_View;
    /// <summary>
    /// Associated logical view 3D
    /// </summary>
    public View3D View
    {
        get
        {
            return m_View;
        }
    }
    /// <summary>
    /// Parent resizable grid
    /// </summary>
    public ResizableGrid ParentGrid { get; set; }
    /// <summary>
    /// GameObject to hide a minimized view
    /// </summary>
    private GameObject m_MinimizedGameObject;
    /// <summary>
    /// Is the view initialized ?
    /// </summary>
    private bool m_IsInitialized = false;
    #endregion

    #region Private Methods
    private void Awake()
    {
        ParentGrid = GetComponentInParent<ResizableGrid>();
    }
    /// <summary>
    /// Get RectTransform screen coordinates
    /// </summary>
    /// <param name="transform">Rect Transform to get screen coordinates from</param>
    /// <returns></returns>
    private Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }
    #endregion

    #region Public Methods
    public void OnRectTransformDimensionsChange()
    {
        if (!m_IsInitialized) return;

        if (Mathf.Abs(GetComponent<RectTransform>().rect.height - ParentGrid.MinimumViewHeight) <= 0.9f)
        {
            if (Mathf.Abs(GetComponent<RectTransform>().rect.width - ParentGrid.MinimumViewWidth) <= 0.9f)
            {
                m_MinimizedGameObject.SetActive(false);
            }
            else
            {
                m_MinimizedGameObject.SetActive(true);
            }
            //m_View.IsMinimized = true;
        }
        else
        {
            m_MinimizedGameObject.SetActive(false);
            //m_View.IsMinimized = false;
        }

        Rect viewport = RectTransformToScreenSpace(GetComponent<RectTransform>());
        m_View.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
    }
    /// <summary>
    /// Initialize this view
    /// </summary>
    public void Initialize(View3D view)
    {
        m_View = view;
        Rect viewport = RectTransformToScreenSpace(GetComponent<RectTransform>());
        m_View.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
        m_MinimizedGameObject = transform.Find("MinimizedImage").gameObject;
        m_MinimizedGameObject.GetComponentInChildren<Text>().text = "View " + view.LineID;
        m_MinimizedGameObject.SetActive(false);
        m_IsInitialized = true;
    }
    #endregion
}
