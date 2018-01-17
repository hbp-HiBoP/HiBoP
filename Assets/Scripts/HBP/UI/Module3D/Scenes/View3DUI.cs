using HBP.Module3D;
using Tools.Unity.ResizableGrid;
using Tools.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using UnityEngine.Events;

public class View3DUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region Properties
    /// <summary>
    /// Associated logical scene 3D
    /// </summary>
    private Base3DScene m_Scene;
    /// <summary>
    /// Associated logical column 3D
    /// </summary>
    private Column3D m_Column;
    /// <summary>
    /// Associated logical view 3D
    /// </summary>
    private View3D m_View;
    /// <summary>
    /// Parent resizable grid
    /// </summary>
    public ResizableGrid ParentGrid { get; set; }
    /// <summary>
    /// GameObject to hide a minimized view
    /// </summary>
    private GameObject m_MinimizedGameObject;
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
    /// <summary>
    /// True if the pointer in on the view ui
    /// </summary>
    private bool m_PointerIsInView;

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

    /// <summary>
    /// Returns true if the view is minimized but the column is not
    /// </summary>
    public bool IsViewMinimizedAndColumnNotMinimized
    {
        get
        {
            return (Mathf.Abs(m_RectTransform.rect.height - ParentGrid.MinimumViewHeight) <= 0.9f) && !(Mathf.Abs(m_RectTransform.rect.width - ParentGrid.MinimumViewWidth) <= 0.9f);
        }
    }
    
    private bool m_RectTransformChanged = false;
    #endregion

    #region Events
    /// <summary>
    /// Event called when changing the minimized state of the view
    /// </summary>
    public UnityEvent OnChangeViewSize = new UnityEvent();
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
        if (m_RectTransformChanged)
        {

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
                if (m_RectTransform.rect.width > 0 && m_RectTransform.rect.height > 0)
                {
                    RenderTexture renderTexture = new RenderTexture((int)m_RectTransform.rect.width, (int)m_RectTransform.rect.height, 24);
                    renderTexture.antiAliasing = 1;
                    m_View.TargetTexture = renderTexture;
                    m_View.Aspect = m_RectTransform.rect.width / m_RectTransform.rect.height;
                    m_RawImage.texture = m_View.TargetTexture;
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }
            else
            {
                Rect viewport = m_RectTransform.ToScreenSpace();
                m_View.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
            }

            OnChangeViewSize.Invoke();
            m_RectTransformChanged = false;
        }
        DeselectView();
        SendRayToScene();
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
    /// <summary>
    /// Transform the mouse position to a ray and send it to the scene
    /// </summary>
    private void SendRayToScene()
    {
        Ray ray;
        if (CursorToRay(out ray))
        {
            m_Scene.PassiveRaycastOnScene(ray, m_Column);
        }
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
            if (Input.GetMouseButton(1))
            {
                Cursor.SetCursor(ApplicationState.GeneralSettings.Theme.General.MoveCursor.Texture, ApplicationState.GeneralSettings.Theme.General.MoveCursor.Offset, CursorMode.ForceSoftware);
            }
            else
            {
                Cursor.SetCursor(ApplicationState.GeneralSettings.Theme.General.MoveCursor.Texture, ApplicationState.GeneralSettings.Theme.General.MoveCursor.Offset, CursorMode.ForceSoftware);
            }
        }
        else
        {
            Ray ray;
            if (CursorToRay(out ray))
            {
                m_Scene.ClickOnScene(ray);
            }
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
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }
    public void OnPointerUp(PointerEventData data)
    {
        m_PointerDownLock = false;
        m_View.DisplayRotationCircles = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }
    public void OnScroll(PointerEventData data)
    {
        m_PointerDownLock = true;
        ROI selectedROI = m_Column.SelectedROI;
        if (m_Scene.SceneInformation.IsROICreationModeEnabled && selectedROI)
        {
            if (selectedROI.SelectedSphereID != -1)
            {
                selectedROI.ChangeSelectedBubbleSize(data.scrollDelta.y);
            }
            else
            {
                m_View.ZoomCamera(data.scrollDelta.y);
            }
        }
        else
        {
            m_View.ZoomCamera(data.scrollDelta.y);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        m_PointerIsInView = true;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        m_PointerIsInView = false;
    }
    public void OnRectTransformDimensionsChange()
    {
        m_RectTransformChanged = true;
    }
    /// <summary>
    /// Initialize this view
    /// </summary>
    public void Initialize(Base3DScene scene, Column3D column, View3D view)
    {
        m_Scene = scene;
        m_Column = column;
        m_View = view;
        
        if (!m_UsingRenderTexture)
        {
            Rect viewport = m_RectTransform.ToScreenSpace();
            m_View.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
        }
        else
        {
            if (m_RectTransform.rect.width > 0 && m_RectTransform.rect.height > 0)
            {
                RenderTexture renderTexture = new RenderTexture((int)m_RectTransform.rect.width, (int)m_RectTransform.rect.height, 24);
                renderTexture.antiAliasing = 1;
                m_View.TargetTexture = renderTexture;
                m_View.Aspect = m_RectTransform.rect.width / m_RectTransform.rect.height;
                m_RawImage.texture = m_View.TargetTexture;
            }
        }
        
        m_MinimizedGameObject = transform.Find("MinimizedImage").gameObject;
        m_MinimizedGameObject.GetComponentInChildren<Text>().text = "View " + view.LineID;
        m_MinimizedGameObject.SetActive(false);
    }
    /// <summary>
    /// Create a ray corresponding to the mouse position in the viewport of the view
    /// </summary>
    /// <param name="ray">Ray to be created</param>
    /// <returns>True if the cursor is indeed in the view</returns>
    public bool CursorToRay(out Ray ray)
    {
        if (!m_PointerIsInView)
        {
            ray = new Ray();
            return false;
        }

        Vector2 localPosition = new Vector2();
        Vector2 position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, position, null, out localPosition);
        localPosition = new Vector2((localPosition.x / m_RectTransform.rect.width) + 0.5f, (localPosition.y / m_RectTransform.rect.height) + 0.5f);
        ray = m_View.Camera.ViewportPointToRay(localPosition);
        return localPosition.x >= 0 && localPosition.x <= 1 && localPosition.y >= 0 && localPosition.y <= 1;
    }
    #endregion
}
