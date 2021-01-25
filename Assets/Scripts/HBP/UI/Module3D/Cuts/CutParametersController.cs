using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class allows the modification of the corresponding cut
    /// </summary>
    public class CutParametersController : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Cut affected to this parameters controller
        /// </summary>
        public Cut Cut { get; private set; }
        /// <summary>
        /// Parent scene of the corresponding cut controller
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Is the parameters controller currently updating
        /// </summary>
        private bool m_IsUIUpdating = false;
        /// <summary>
        /// Are the UI elements allowing the modification of the cut visible ?
        /// </summary>
        public bool AreControlsOpen { get; set; }
        /// <summary>
        /// Texture of the cut associated with this controller
        /// </summary>
        public Texture2D Texture { get { return m_Image.sprite.texture; } }

        /// <summary>
        /// Did we just clicked on the minus button ?
        /// </summary>
        private bool m_ClickedOnMinus = false;
        /// <summary>
        /// Did we just clicked on the plus button ?
        /// </summary>
        private bool m_ClickedOnPlus = false;
        /// <summary>
        /// Time since last position update if the mouse is being held on the minus or plus button
        /// </summary>
        private float m_TimeSinceLastUpdate = 0.0f;
        /// <summary>
        /// Time between two position updates if the mouse is being held on the minus or plus button
        /// </summary>
        private float m_TimeBetweenTwoUpdates = 0.1f;
        /// <summary>
        /// Has the object been requested to be destroyed ?
        /// </summary>
        private bool m_Destroyed = false;

        /// <summary>
        /// Image of the cut (to hold the texture of the cut)
        /// </summary>
        [SerializeField] private Image m_Image;
        /// <summary>
        /// Dropdown to change the orientation of the cut
        /// </summary>
        /// <remarks>
        /// <seealso cref="Data.Enums.CutOrientation"/>
        /// </remarks>
        [SerializeField] private Dropdown m_Orientation;
        /// <summary>
        /// Slider to change the position of the cut
        /// </summary>
        [SerializeField] private Slider m_Position;
        /// <summary>
        /// Button to slightly increase the position of the cut
        /// </summary>
        [SerializeField] private PressableButton m_PlusPosition;
        /// <summary>
        /// Button to slightly decrease the position of the cut
        /// </summary>
        [SerializeField] private PressableButton m_MinusPosition;
        /// <summary>
        /// Toggle to change the flip of the cut
        /// </summary>
        [SerializeField] private Toggle m_Flip;
        /// <summary>
        /// Button to remove the cut from the scene
        /// </summary>
        [SerializeField] private Button m_Remove;
        /// <summary>
        /// RectTransform of the custom cut editor (allows to completely set the normal of the cut plane)
        /// </summary>
        [SerializeField] private RectTransform m_CustomValues;
        /// <summary>
        /// X value of the custom normal
        /// </summary>
        [SerializeField] private InputField m_CustomX;
        /// <summary>
        /// Y value of the custom normal
        /// </summary>
        [SerializeField] private InputField m_CustomY;
        /// <summary>
        /// Z value of the custom normal
        /// </summary>
        [SerializeField] private InputField m_CustomZ;
        /// <summary>
        /// Text on the MRI for the direction of the cut (for non-custom orientations)
        /// </summary>
        [SerializeField] private Text m_PositionTitle;
        /// <summary>
        /// Text on the MRI for the value of the cut (for non-custom orientations)
        /// </summary>
        [SerializeField] private Text m_PositionValue;
        /// <summary>
        /// Gameobject showing information about the position of the cut (for non-custom orientations)
        /// </summary>
        [SerializeField] private GameObject m_PositionInformation;

        /// <summary>
        /// Parent RectTransform for the cut sites
        /// </summary>
        [SerializeField] private RectTransform m_SitesRectTransform;
        /// <summary>
        /// Prefab for the cut sites
        /// </summary>
        [SerializeField] private GameObject m_SitePrefab;

        /// <summary>
        /// Parent RectTransform for the other cuts lines
        /// </summary>
        [SerializeField] private RectTransform m_CutLinesRectTransform;
        /// <summary>
        /// Prefab for the others cuts lines
        /// </summary>
        [SerializeField] private GameObject m_CutLinePrefab;
        #endregion

        #region Events
        /// <summary>
        /// Event called when opening the UI elements allowing to modify the cut
        /// </summary>
        public UnityEvent OnOpenControls = new UnityEvent();
        /// <summary>
        /// Event called when closing the UI elements allowing to modify the cut
        /// </summary>
        public UnityEvent OnCloseControls = new UnityEvent();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_ClickedOnPlus || m_ClickedOnMinus)
            {
                m_TimeSinceLastUpdate += Time.deltaTime;
                if (m_TimeSinceLastUpdate > m_TimeBetweenTwoUpdates)
                {
                    m_TimeSinceLastUpdate = 0;
                    if (m_ClickedOnPlus)
                    {
                        m_Position.value += 1.0f / Cut.NumberOfCuts;
                    }
                    else if (m_ClickedOnMinus)
                    {
                        m_Position.value -= 1.0f / Cut.NumberOfCuts;
                    }
                }
            }
            
        }
        /// <summary>
        /// Add all listeners from UI to 3D scene and inversely
        /// </summary>
        private void AddListeners()
        {
            Cut.OnUpdateGUITextures.AddListener((column) =>
            {
                Destroy(m_Image.sprite);
                Texture2D texture = column.CutTextures.GUIBrainCutTextures[Cut.ID];
                m_Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                m_Image.sprite.texture.filterMode = FilterMode.Trilinear;
                m_Image.sprite.texture.anisoLevel = 9;
            });
            m_Scene.OnUpdateCuts.AddListener(() =>
            {
                if (m_Destroyed) return;

                UpdateUI();
                ShowSites();
                DrawLines();
            });
            Cut.OnRemoveCut.AddListener(() =>
            {
                Destroy(gameObject);
                m_Destroyed = true;
            });

            m_Position.onValueChanged.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                Cut.Position = value;
                m_Scene.UpdateCutPlane(Cut, true);
            });
            m_MinusPosition.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Position.value -= 1.0f / Cut.NumberOfCuts;
            });
            m_MinusPosition.onPress.AddListener(() =>
            {
                m_ClickedOnMinus = true;
                m_TimeSinceLastUpdate = -0.5f;
            });
            m_MinusPosition.onRelease.AddListener(() =>
            {
                m_ClickedOnMinus = false;
            });
            m_PlusPosition.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Position.value += 1.0f / Cut.NumberOfCuts;
            });
            m_PlusPosition.onPress.AddListener(() =>
            {
                m_ClickedOnPlus = true;
                m_TimeSinceLastUpdate = -0.5f;
            });
            m_PlusPosition.onRelease.AddListener(() =>
            {
                m_ClickedOnPlus = false;
            });
            m_Orientation.onValueChanged.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                Cut.Orientation = (Data.Enums.CutOrientation)value;
                if (Cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomX.text, out float x);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomY.text, out float y);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomZ.text, out float z);
                    Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(Cut, true);
            });
            m_Flip.onValueChanged.AddListener((isOn) =>
            {
                if (m_IsUIUpdating) return;

                Cut.Flip = isOn;
                Cut.Position = 1.0f - Cut.Position;
                m_Scene.UpdateCutPlane(Cut, true);
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Scene.RemoveCutPlane(Cut);
            });
            m_CustomX.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (Cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomX.text, out float x);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomY.text, out float y);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomZ.text, out float z);
                    Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(Cut, true);
            });
            m_CustomY.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (Cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomX.text, out float x);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomY.text, out float y);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomZ.text, out float z);
                    Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(Cut, true);
            });
            m_CustomZ.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (Cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomX.text, out float x);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomY.text, out float y);
                    global::Tools.CSharp.NumberExtension.TryParseFloat(m_CustomZ.text, out float z);
                    Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(Cut, true);
            });
            m_Image.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (AreControlsOpen)
                {
                    CloseControls();
                }
                else
                {
                    OpenControls();
                }
            });
            m_Scene.OnSitesRenderingUpdated.AddListener(() =>
            {
                if (m_Destroyed) return;

                ShowSites();
            });
        }
        /// <summary>
        /// Update the values in the UI from the values of the corresponding cut
        /// </summary>
        private void UpdateUI()
        {
            m_IsUIUpdating = true;
            if (AreControlsOpen)
            {
                m_Position.value = Cut.Position;
                m_Orientation.value = (int)Cut.Orientation;
                m_Orientation.RefreshShownValue();
                m_Flip.isOn = Cut.Flip;
                m_CustomX.text = Cut.Normal.x.ToString();
                m_CustomY.text = Cut.Normal.y.ToString();
                m_CustomZ.text = Cut.Normal.z.ToString();
                m_Remove.gameObject.SetActive(true);
                m_Orientation.gameObject.SetActive(true);
                m_Position.transform.parent.gameObject.SetActive(true);

                m_Flip.gameObject.SetActive(Cut.Orientation != Data.Enums.CutOrientation.Custom);
                m_CustomValues.gameObject.SetActive(Cut.Orientation == Data.Enums.CutOrientation.Custom);
            }
            else
            {
                m_Remove.gameObject.SetActive(false);
                m_Orientation.gameObject.SetActive(false);
                m_Position.transform.parent.gameObject.SetActive(false);
                m_Flip.gameObject.SetActive(false);
                m_CustomValues.gameObject.SetActive(false);
            }
            m_PositionInformation.SetActive(Cut.Orientation != Data.Enums.CutOrientation.Custom);
            switch (Cut.Orientation)
            {
                case Data.Enums.CutOrientation.Axial:
                    m_PositionTitle.text = "Z";
                    m_PositionValue.text = Mathf.RoundToInt(Cut.Point.z).ToString();
                    break;
                case Data.Enums.CutOrientation.Coronal:
                    m_PositionTitle.text = "Y";
                    m_PositionValue.text = Mathf.RoundToInt(Cut.Point.y).ToString();
                    break;
                case Data.Enums.CutOrientation.Sagittal:
                    m_PositionTitle.text = "X";
                    m_PositionValue.text = Mathf.RoundToInt(Cut.Point.x).ToString();
                    break;
            }
            m_IsUIUpdating = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this controller
        /// </summary>
        /// <param name="scene">Parent scene of the cut controller</param>
        /// <param name="cut">Cut associated to this controller</param>
        public void Initialize(Base3DScene scene, Cut cut)
        {
            m_Scene = scene;
            Cut = cut;
            m_Image.GetComponent<global::Tools.Unity.Components.ImageRatio>().Type = global::Tools.Unity.Components.ImageRatio.ControlType.WidthControlsHeight;
            m_Orientation.options = new List<Dropdown.OptionData>();
            foreach (var orientation in Enum.GetNames(typeof(Data.Enums.CutOrientation)))
            {
                m_Orientation.options.Add(new Dropdown.OptionData(orientation));
            }
            UpdateUI();
            AddListeners();
        }
        /// <summary>
        /// Open the UI elements allowing to modify the cut
        /// </summary>
        public void OpenControls()
        {
            AreControlsOpen = true;
            UpdateUI();
            OnOpenControls.Invoke();
        }
        /// <summary>
        /// Close the UI elements allowing to modify the cut
        /// </summary>
        public void CloseControls()
        {
            AreControlsOpen = false;
            UpdateUI();
            OnCloseControls.Invoke();
        }
        /// <summary>
        /// Show the sites on the cut image
        /// </summary>
        public void ShowSites()
        {
            foreach (Transform child in m_SitesRectTransform) Destroy(child.gameObject);
            if (Cut.Orientation == Data.Enums.CutOrientation.Custom) return;
            
            List<Site> sites = new List<Site>();
            m_Scene.SelectedColumn.RawElectrodes.GetSitesOnPlane(Cut, 1.0f, out int[] result);
            foreach (var site in m_Scene.SelectedColumn.Sites)
            {
                if (result[site.Information.Index] == 1 && site.IsActive)
                {
                    sites.Add(site);
                }
            }

            foreach (var site in sites)
            {
                Vector2 ratio = m_Scene.CutGeometryGenerators[Cut.ID].GetPositionRatioOnTexture(site.transform.localPosition);
                float horizontalRatio = 0, verticalRatio = 0;
                switch (Cut.Orientation)
                {
                    case Data.Enums.CutOrientation.Axial:
                        horizontalRatio = Cut.Flip ? 1.0f - ratio.x : ratio.x;
                        verticalRatio = ratio.y;
                        break;
                    case Data.Enums.CutOrientation.Coronal:
                        horizontalRatio = Cut.Flip ? 1.0f - ratio.x : ratio.x;
                        verticalRatio = Cut.Flip ? 1.0f - ratio.y : ratio.y;
                        break;
                    case Data.Enums.CutOrientation.Sagittal:
                        horizontalRatio = Cut.Flip ? 1.0f - ratio.y : ratio.y;
                        verticalRatio = Cut.Flip ? ratio.x : 1.0f - ratio.x;
                        break;
                }

                CutSite cutSite = Instantiate(m_SitePrefab, m_SitesRectTransform).GetComponent<CutSite>();
                cutSite.Initialize(m_Scene, site, new Vector2(horizontalRatio, verticalRatio));
            }
        }
        /// <summary>
        /// Draw the cut lines of other cuts on the cut image
        /// </summary>
        public void DrawLines()
        {
            foreach (Transform child in m_CutLinesRectTransform) Destroy(child.gameObject);
            if (Cut.Orientation == Data.Enums.CutOrientation.Custom || !ApplicationState.UserPreferences.Visualization.Cut.ShowCutLines) return;

            HBP.Module3D.DLL.BBox boundingBox = m_Scene.CutGeometryGenerators[Cut.ID].BoundingBox;
            if (boundingBox != null)
            {
                Vector3 min = boundingBox.Min;
                Vector3 max = boundingBox.Max;

                foreach (var cut in m_Scene.Cuts)
                {
                    if (cut == Cut || cut.Orientation == Data.Enums.CutOrientation.Custom) continue;

                    Segment3 segment = boundingBox.IntersectionSegmentBetweenTwoPlanes(Cut, cut);
                    List<Vector2> linePoints = new List<Vector2>();
                    if (segment != null)
                    {
                        void addRatioOfPoint(Vector3 point)
                        {
                            Vector2 ratio = m_Scene.CutGeometryGenerators[Cut.ID].GetPositionRatioOnTexture(new Vector3(-point.x, point.y, point.z));
                            float horizontalRatio = 0, verticalRatio = 0;
                            switch (Cut.Orientation)
                            {
                                case Data.Enums.CutOrientation.Axial:
                                    horizontalRatio = Cut.Flip ? 1.0f - ratio.x : ratio.x;
                                    verticalRatio = ratio.y;
                                    break;
                                case Data.Enums.CutOrientation.Coronal:
                                    horizontalRatio = Cut.Flip ? 1.0f - ratio.x : ratio.x;
                                    verticalRatio = Cut.Flip ? 1.0f - ratio.y : ratio.y;
                                    break;
                                case Data.Enums.CutOrientation.Sagittal:
                                    horizontalRatio = Cut.Flip ? 1.0f - ratio.y : ratio.y;
                                    verticalRatio = Cut.Flip ? ratio.x : 1.0f - ratio.x;
                                    break;
                            }
                            linePoints.Add(new Vector2(horizontalRatio, verticalRatio));
                        }
                        addRatioOfPoint(segment.End1);
                        addRatioOfPoint(segment.End2);
                    }
                    UILineRenderer lineRenderer = Instantiate(m_CutLinePrefab, m_CutLinesRectTransform).GetComponent<UILineRenderer>();
                    RectTransform lineRectTransform = lineRenderer.GetComponent<RectTransform>();
                    lineRectTransform.anchorMin = Vector2.zero;
                    lineRectTransform.anchorMax = Vector2.one;
                    lineRectTransform.anchoredPosition = Vector2.zero;
                    lineRectTransform.sizeDelta = Vector2.zero;
                    lineRenderer.Points = linePoints.ToArray();
                }
            }
        }
        #endregion
    }
}