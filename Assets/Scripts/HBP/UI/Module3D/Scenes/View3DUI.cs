using HBP.Module3D;
using Tools.Unity.ResizableGrid;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Theme.Components;
using System.Collections.Generic;
using System.Linq;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class is used to properly display a view 3D in the UI
    /// </summary>
    public class View3DUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// If the difference between the width of the view and the default minimum width of a column in the ResizableGrid is less than this value, it is considered minimized
        /// </summary>
        private const float MINIMIZED_THRESHOLD = 10.0f;

        /// <summary>
        /// Corresponding theme element (to display when the view is selected)
        /// </summary>
        [SerializeField] private ThemeElement m_ThemeElement;
        /// <summary>
        /// Theme Element state used when displaying the cursor indicating that the brain is moving or rotating
        /// </summary>
        [SerializeField] private State m_MoveState;
        /// <summary>
        /// Reference to the selection ring used to display which site is currently selected in this view
        /// </summary>
        [SerializeField] private SelectionRing m_SelectionRing;
        /// <summary>
        /// Parent containing all correlation rings
        /// </summary>
        [SerializeField] private RectTransform m_CorrelationRingsParent;
        /// <summary>
        /// Prefab of the correlation ring
        /// </summary>
        [SerializeField] private GameObject m_CorrelationRingPrefab;
        /// <summary>
        /// Prefab for the correlation ring of the selected site for correlations
        /// </summary>
        [SerializeField] private GameObject m_BaseCorrelationRingPrefab;

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
        /// RawImage to display the RenderTexture of the Camera
        /// </summary>
        private RawImage m_RawImage;
        /// <summary>
        /// Reference to the RectTransform of this object
        /// </summary>
        private RectTransform m_RectTransform;
        /// <summary>
        /// True if the pointer in on the view UI
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
                m_RectTransform.hasChanged = true;
            }
        }

        /// <summary>
        /// Is the view minimized horizontally ?
        /// </summary>
        public bool IsMinimizedHorizontally
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - ParentGrid.MinimumViewWidth) <= MINIMIZED_THRESHOLD;
            }
        }
        /// <summary>
        /// Is the view minimized vertically ?
        /// </summary>
        public bool IsMinimzedVertically
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.height - ParentGrid.MinimumViewHeight) <= MINIMIZED_THRESHOLD;
            }
        }
        /// <summary>
        /// Returns true if the view is minimized but the column is not
        /// </summary>
        public bool IsViewMinimizedAndColumnNotMinimized
        {
            get
            {
                return IsMinimzedVertically && !IsMinimizedHorizontally;
            }
        }
        /// <summary>
        /// Is the view minimized vertically or horizontally ?
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return IsMinimizedHorizontally || IsMinimzedVertically;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing the size of the view
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
            if (m_RectTransform.hasChanged)
            {
                m_MinimizedGameObject.SetActive(IsViewMinimizedAndColumnNotMinimized);
                m_View.IsMinimized = IsMinimized;

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
                m_RectTransform.hasChanged = false;
            }
            SendRayToScene();
        }
        /// <summary>
        /// Transform the mouse position to a ray and send it to the scene
        /// </summary>
        private void SendRayToScene()
        {
            if (CursorToRay(out Ray ray))
            {
                m_Scene.PassiveRaycastOnScene(ray, m_Column);
            }
        }
        /// <summary>
        /// Callback method when selecting a site
        /// </summary>
        /// <param name="site">Site that has been selected</param>
        private void OnSelectSite(Core.Object3D.Site site)
        {
            m_SelectionRing.Site = site;
            UpdateCorrelationsOverlay();
        }
        /// <summary>
        /// Update the overlay circles to display the correlations between the selected site of the column and all other sites
        /// </summary>
        private void UpdateCorrelationsOverlay()
        {
            if (m_Column is Column3DIEEG column)
            {
                foreach (Transform transfo in m_CorrelationRingsParent)
                {
                    Destroy(transfo.gameObject);
                }
                if (m_Scene.DisplayCorrelations)
                {
                    // If using the CompareSite feature, keep the site to compare focused
                    Core.Object3D.Site baseSite = m_Scene.ImplantationManager.SiteToCompare != null ? m_Scene.ImplantationManager.SiteToCompare : column.SelectedSite;
                    if (baseSite != null)
                    {
                        foreach (var correlatedSite in column.CorrelatedSites(baseSite))
                        {
                            SelectionRing ring = Instantiate(m_CorrelationRingPrefab, m_CorrelationRingsParent).GetComponent<SelectionRing>();
                            ring.ViewCamera = m_View.Camera;
                            ring.Viewport = m_RectTransform;
                            ring.Site = correlatedSite;
                        }
                        SelectionRing baseRing = Instantiate(m_BaseCorrelationRingPrefab, m_CorrelationRingsParent).GetComponent<SelectionRing>();
                        baseRing.ViewCamera = m_View.Camera;
                        baseRing.Viewport = m_RectTransform;
                        baseRing.Site = baseSite;
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void OnPointerDown(PointerEventData data)
        {
            if (IsMinimized) return;

            m_View.IsSelected = true;
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                m_View.DisplayRotationCircles = true;
                m_ThemeElement.Set(m_MoveState);
            }
            else
            {
                if (CursorToRay(out Ray ray))
                {
                    m_Scene.ClickOnScene(ray);
                }
            }
        }
        public void OnDrag(PointerEventData data)
        {
            if (IsMinimized) return;

            switch (data.button)
            {
                case PointerEventData.InputButton.Left:
                    if (m_Scene.ROIManager.ROICreationMode)
                    {
                        m_Scene.ROIManager.MoveSelectedROISphere(m_View.Camera, data.delta);
                    }
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
            if (IsMinimized) return;

            m_View.DisplayRotationCircles = false;
            m_ThemeElement.Set();
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (IsMinimized) return;

            m_View.DisplayRotationCircles = false;
            m_ThemeElement.Set();
        }
        public void OnScroll(PointerEventData data)
        {
            if (IsMinimized) return;

            Core.Object3D.ROI selectedROI = m_Scene.ROIManager.SelectedROI;
            if (m_Scene.ROIManager.ROICreationMode && selectedROI)
            {
                if (selectedROI.SelectedSphereID != -1)
                {
                    selectedROI.ChangeSelectedSphereSize(data.scrollDelta.y);
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
            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new Core.Object3D.SiteInfo(null, false, Input.mousePosition));
        }
        /// <summary>
        /// Initialize the View3DUI
        /// </summary>
        /// <param name="scene">Parent scene of the associated view</param>
        /// <param name="column">Parent column of the associated view</param>
        /// <param name="view">Associated logical view</param>
        public void Initialize(Base3DScene scene, Column3D column, View3D view)
        {
            m_Scene = scene;
            m_Column = column;
            m_View = view;
            ParentGrid = GetComponentInParent<ResizableGrid>();
            m_RectTransform = GetComponent<RectTransform>();
            m_RawImage = GetComponent<RawImage>();
            UsingRenderTexture = true;

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

            m_SelectionRing.ViewCamera = view.Camera;
            m_SelectionRing.Viewport = m_RectTransform;
            m_Column.OnSelectSite.AddListener(OnSelectSite);
            m_Scene.OnChangeDisplayCorrelations.AddListener(UpdateCorrelationsOverlay);
            OnSelectSite(m_Column.SelectedSite);
        }
        /// <summary>
        /// Create a ray corresponding to the mouse position in the viewport of the view
        /// </summary>
        /// <param name="ray">Ray to be created</param>
        /// <returns>True if the cursor is in the view</returns>
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
}