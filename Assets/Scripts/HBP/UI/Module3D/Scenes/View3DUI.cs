using HBP.Module3D;
using Tools.Unity.ResizableGrid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class View3DUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler, IScrollHandler {
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
    /// <summary>
    /// Lock to know whether a click triggered OnPointerDown event or not
    /// </summary>
    private bool m_PointerDownLock;

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
        DeselectView();
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
    private void DeselectView()
    {
        UnityEngine.Profiling.Profiler.BeginSample("check click");
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.mouseScrollDelta != Vector2.zero)
        {
            if (!m_PointerDownLock) // If a click was recorded but this click did not trigger OnPointerDown, consider that the view has not been clicked
            {
                m_View.IsClicked = false;
            }
        }
        m_PointerDownLock = false;
        UnityEngine.Profiling.Profiler.EndSample();
    }
    #endregion

    #region Public Methods
    public void OnPointerDown(PointerEventData data)
    {
        m_View.IsClicked = true;
        m_PointerDownLock = true;
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            m_View.DisplayRotationCircles = true;
        }
    }
    public void OnDrag(PointerEventData data)
    {
        switch (data.button)
        {
            case PointerEventData.InputButton.Left:
                break;
            case PointerEventData.InputButton.Right:
                m_View.RotateCamera(data.delta);
                break;
            case PointerEventData.InputButton.Middle:
                m_View.StrafeCamera(data.delta);
                break;
            default:
                break;
        }
    }
    public void OnEndDrag(PointerEventData data)
    {
        m_View.DisplayRotationCircles = false;
    }
    public void OnPointerUp(PointerEventData data)
    {
        m_PointerDownLock = false;
        m_View.DisplayRotationCircles = false;
    }
    public void OnScroll(PointerEventData data)
    {
        m_View.IsClicked = true;
        m_PointerDownLock = true;
        m_View.ZoomCamera(data.scrollDelta.y);
    }
    public void OnRectTransformDimensionsChange()
    {
        if (!m_IsInitialized) return; // if not initialized, don't do anything

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
            if (m_RectTransform.rect.width <= 0 || m_RectTransform.rect.height <= 0) // If the user drags too fast and width or height are negative or zero, do not continue;
            {
                return;
            }
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
    /// <summary>
    /// Get ray from PointerEventData to the camera of the view
    /// </summary>
    /// <param name="data">Pointer event data from OnPointerDown or similar</param>
    /// <returns>Ray obtained from conversion</returns>
    public Ray RaycastToCamera(PointerEventData data)
    {
        Vector2 localPosition = new Vector2();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, data.position, null, out localPosition);
        localPosition = new Vector2((localPosition.x / m_RectTransform.rect.width) + 0.5f, (localPosition.y / m_RectTransform.rect.height) + 0.5f);
        Ray ray = m_View.Camera.ViewportPointToRay(localPosition);
        RaycastHit hit = new RaycastHit();
        Physics.Raycast(ray, out hit);
        return ray;
    }
    #endregion
}
