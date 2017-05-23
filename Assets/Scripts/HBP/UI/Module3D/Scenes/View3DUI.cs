using HBP.Module3D;
using Tools.Unity.ResizableGrid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

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
    /// <summary>
    /// Render camera texture
    /// </summary>
    private RawImage m_RawImage;
    /// <summary>
    /// Rect Transform
    /// </summary>
    private RectTransform m_RectTransform;

    private bool m_UsingRenderTexture;
    /// <summary>
    /// True if we are using render textures for the cameras (instead of changing the viewport)
    /// </summary>
    public bool UsingRenderTexture
    {
        get
        {
            return m_UsingRenderTexture;
        }
        set
        {
            m_UsingRenderTexture = value;
            m_RawImage.enabled = value;
            OnRectTransformDimensionsChange();
        }
    }
    #endregion

    #region Private Methods
    private void Awake()
    {
        ParentGrid = GetComponentInParent<ResizableGrid>();
        m_RectTransform = GetComponent<RectTransform>();
        m_RawImage = GetComponent<RawImage>();
        UsingRenderTexture = true;
    }
    private void Update()
    {
        MouseHandler();
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
    /// <summary>
    /// Handles mouse events
    /// </summary>
    private void MouseHandler()
    {
        UnityEngine.Profiling.Profiler.BeginSample("check click");
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            float x = Input.mousePosition.x, y = Input.mousePosition.y;
            Rect rect = RectTransformToScreenSpace(m_RectTransform);
            m_View.IsClicked = (x > rect.x && x < rect.x + rect.width && y > rect.y && y < rect.y + rect.height);
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }
    #endregion

    #region Public Methods
    public void OnRectTransformDimensionsChange()
    {
        if (!m_IsInitialized) return;

        if (Mathf.Abs(m_RectTransform.rect.height - ParentGrid.MinimumViewHeight) <= 0.9f)
        {
            if (Mathf.Abs(m_RectTransform.rect.width - ParentGrid.MinimumViewWidth) <= 0.9f)
            {
                m_MinimizedGameObject.SetActive(false);
            }
            else
            {
                m_MinimizedGameObject.SetActive(true);
            }
            m_View.IsMinimized = true;
        }
        else if (Mathf.Abs(m_RectTransform.rect.width - ParentGrid.MinimumViewWidth) <= 0.9f)
        {
            m_View.IsMinimized = true;
        }
        else
        {
            m_MinimizedGameObject.SetActive(false);
            m_View.IsMinimized = false;
        }
        
        if (m_UsingRenderTexture)
        {
            UnityEngine.Profiling.Profiler.BeginSample("RenderTexture");
            RenderTexture renderTexture = new RenderTexture((int)m_RectTransform.rect.width, (int)m_RectTransform.rect.height, 24);
            renderTexture.antiAliasing = 1;
            m_View.TargetTexture = renderTexture;
            m_View.Aspect = m_RectTransform.rect.width / m_RectTransform.rect.height;
            m_RawImage.texture = m_View.TargetTexture;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        else
        {
            Rect viewport = RectTransformToScreenSpace(m_RectTransform);
            m_View.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
        }
    }
    /// <summary>
    /// Initialize this view
    /// </summary>
    public void Initialize(View3D view)
    {
        m_View = view;
        
        if (!m_UsingRenderTexture)
        {
            Rect viewport = RectTransformToScreenSpace(m_RectTransform);
            m_View.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
        }
        else
        {
            RenderTexture renderTexture = new RenderTexture((int)m_RectTransform.rect.width, (int)m_RectTransform.rect.height, 24);
            renderTexture.antiAliasing = 1;
            m_View.TargetTexture = renderTexture;
            m_View.Aspect = m_RectTransform.rect.width / m_RectTransform.rect.height;
            m_RawImage.texture = m_View.TargetTexture;
        }
        
        m_MinimizedGameObject = transform.Find("MinimizedImage").gameObject;
        m_MinimizedGameObject.GetComponentInChildren<Text>().text = "View " + view.LineID;
        m_MinimizedGameObject.SetActive(false);
        m_IsInitialized = true;
    }
    #endregion
}
