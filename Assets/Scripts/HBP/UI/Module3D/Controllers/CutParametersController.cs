using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public class CutParametersController : MonoBehaviour
    {
        #region Properties
        private Cut m_Cut;
        private Base3DScene m_Scene;
        private bool m_IsUIUpdating = false;
        public bool AreControlsOpen { get; set; }
        public Texture2D Texture { get { return m_Image.sprite.texture; } }

        /// <summary>
        /// Image of the cut
        /// </summary>
        [SerializeField] private Image m_Image;
        /// <summary>
        /// Dropdown to change the axis of the cut
        /// </summary>
        [SerializeField] private Dropdown m_Orientation;
        /// <summary>
        /// Slider to change the position of the cut
        /// </summary>
        [SerializeField] private Slider m_Position;
        /// <summary>
        /// Button to slightly change the position of the cut
        /// </summary>
        [SerializeField] private Button m_PlusPosition;
        /// <summary>
        /// Button to slightly change the position of the cut
        /// </summary>
        [SerializeField] private Button m_MinusPosition;
        /// <summary>
        /// Toggle to change the flip of the cut
        /// </summary>
        [SerializeField] private Toggle m_Flip;
        /// <summary>
        /// Button to remove the cut
        /// </summary>
        [SerializeField] private Button m_Remove;
        /// <summary>
        /// Rect Transform of the custom vector editor
        /// </summary>
        [SerializeField] private RectTransform m_CustomValues;
        /// <summary>
        /// X value for the custom normal
        /// </summary>
        [SerializeField] private InputField m_CustomX;
        /// <summary>
        /// Y value for the custom normal
        /// </summary>
        [SerializeField] private InputField m_CustomY;
        /// <summary>
        /// Z value for the custom normal
        /// </summary>
        [SerializeField] private InputField m_CustomZ;
        /// <summary>
        /// Text on the MRI for the direction of the cut
        /// </summary>
        [SerializeField] private Text m_PositionTitle;
        /// <summary>
        /// Text on the MRI for the value of the cut
        /// </summary>
        [SerializeField] private Text m_PositionValue;
        /// <summary>
        /// Gameobject showing information about the position of the cut
        /// </summary>
        [SerializeField] private GameObject m_PositionInformation;

        [SerializeField] private RectTransform m_SitesRectTransform;
        /// <summary>
        /// Prefab for the sites
        /// </summary>
        [SerializeField] private GameObject m_SitePrefab;
        #endregion

        #region Events
        public UnityEvent OnOpenControls = new UnityEvent();
        public UnityEvent OnCloseControls = new UnityEvent();
        #endregion

        #region Private Methods
        private void AddListeners()
        {
            m_Cut.OnUpdateGUITextures.AddListener((column) =>
            {
                Destroy(m_Image.sprite);
                Texture2D texture = column.GUIBrainCutTextures[m_Cut.ID];
                m_Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                m_Image.sprite.texture.filterMode = FilterMode.Trilinear;
                m_Image.sprite.texture.anisoLevel = 9;
            });
            m_Cut.OnUpdateCut.AddListener(() =>
            {
                UpdateUI();
                ShowSites();
            });
            m_Cut.OnRemoveCut.AddListener(() =>
            {
                Destroy(gameObject);
            });

            m_Position.onValueChanged.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                m_Cut.Position = value;
                m_Scene.UpdateCutPlane(m_Cut);
            });
            m_MinusPosition.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Position.value -= 1.0f / m_Cut.NumberOfCuts;
            });
            m_PlusPosition.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Position.value += 1.0f / m_Cut.NumberOfCuts;
            });
            m_Orientation.onValueChanged.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                m_Cut.Orientation = (CutOrientation)value;
                if (m_Cut.Orientation == CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    m_Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(m_Cut);
            });
            m_Flip.onValueChanged.AddListener((isOn) =>
            {
                if (m_IsUIUpdating) return;

                m_Cut.Flip = isOn;
                m_Cut.Position = 1.0f - m_Cut.Position;
                m_Scene.UpdateCutPlane(m_Cut);
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Scene.RemoveCutPlane(m_Cut);
            });
            m_CustomX.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (m_Cut.Orientation == CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    m_Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(m_Cut);
            });
            m_CustomY.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (m_Cut.Orientation == CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    m_Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(m_Cut);
            });
            m_CustomZ.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (m_Cut.Orientation == CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    m_Cut.Normal = new Vector3(x, y, z);
                }
                m_Scene.UpdateCutPlane(m_Cut);
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
        }
        private void UpdateUI()
        {
            m_IsUIUpdating = true;
            if (AreControlsOpen)
            {
                m_Position.value = m_Cut.Position;
                m_Orientation.value = (int)m_Cut.Orientation;
                m_Orientation.RefreshShownValue();
                m_Flip.isOn = m_Cut.Flip;
                m_CustomX.text = m_Cut.Normal.x.ToString();
                m_CustomY.text = m_Cut.Normal.y.ToString();
                m_CustomZ.text = m_Cut.Normal.z.ToString();
                m_Remove.gameObject.SetActive(true);
                m_Orientation.gameObject.SetActive(true);
                m_Position.transform.parent.gameObject.SetActive(true);
                m_Flip.gameObject.SetActive(m_Cut.Orientation != CutOrientation.Custom);
                m_CustomValues.gameObject.SetActive(m_Cut.Orientation == CutOrientation.Custom);
                m_PositionInformation.SetActive(m_Cut.Orientation != CutOrientation.Custom);
                switch (m_Cut.Orientation)
                {
                    case CutOrientation.Axial:
                        m_PositionTitle.text = "Z";
                        m_PositionValue.text = Mathf.RoundToInt(m_Cut.Point.z).ToString();
                        break;
                    case CutOrientation.Coronal:
                        m_PositionTitle.text = "Y";
                        m_PositionValue.text = Mathf.RoundToInt(m_Cut.Point.y).ToString();
                        break;
                    case CutOrientation.Sagital:
                        m_PositionTitle.text = "X";
                        m_PositionValue.text = Mathf.RoundToInt(m_Cut.Point.x).ToString();
                        break;
                }
            }
            else
            {
                m_Remove.gameObject.SetActive(false);
                m_Orientation.gameObject.SetActive(false);
                m_Position.transform.parent.gameObject.SetActive(false);
                m_Flip.gameObject.SetActive(false);
                m_CustomValues.gameObject.SetActive(false);
            }
            m_IsUIUpdating = false;
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Cut cut)
        {
            m_Scene = scene;
            m_Cut = cut;
            m_Image.GetComponent<ImageRatio>().Type = ImageRatio.RatioType.FixedWidth;
            m_Orientation.options = new List<Dropdown.OptionData>();
            foreach (var orientation in Enum.GetNames(typeof(CutOrientation)))
            {
                m_Orientation.options.Add(new Dropdown.OptionData(orientation));
            }
            UpdateUI();
            AddListeners();
        }
        public void OpenControls()
        {
            AreControlsOpen = true;
            UpdateUI();
            OnOpenControls.Invoke();
        }
        public void CloseControls()
        {
            AreControlsOpen = false;
            UpdateUI();
            OnCloseControls.Invoke();
        }
        public void ShowSites()
        {
            foreach (Transform child in m_SitesRectTransform) Destroy(child.gameObject);
            if (m_Cut.Orientation == CutOrientation.Custom) return;
            
            List<Site> sites = new List<Site>();
            int[] result;
            m_Scene.ColumnManager.SelectedColumn.RawElectrodes.GetSitesOnPlane(m_Cut, 1.0f, out result);
            foreach (var site in m_Scene.ColumnManager.SelectedColumn.Sites)
            {
                if (result[site.Information.GlobalID] == 1 && site.IsActive)
                {
                    sites.Add(site);
                }
            }

            HBP.Module3D.DLL.BBox cube = m_Scene.ColumnManager.CubeBoundingBox;
            List<Vector3> intersections = cube.IntersectionPointsWithPlane(m_Cut);
            float xMax = float.MinValue, yMax = float.MinValue, zMax = float.MinValue;
            float xMin = float.MaxValue, yMin = float.MaxValue, zMin = float.MaxValue;
            foreach (var point in intersections)
            {
                if (point.x > xMax) xMax = point.x;
                if (point.y > yMax) yMax = point.y;
                if (point.z > zMax) zMax = point.z;
                if (point.x < xMin) xMin = point.x;
                if (point.y < yMin) yMin = point.y;
                if (point.z < zMin) zMin = point.z;
            }
            float xRange = xMax - xMin;
            float yRange = yMax - yMin;
            float zRange = zMax - zMin;

            foreach (var site in sites)
            {
                float horizontalRatio = 0, verticalRatio = 0;
                switch (m_Cut.Orientation)
                {
                    case CutOrientation.Axial:
                        horizontalRatio = 1 - ((site.transform.position.x - xMin) / xRange);
                        verticalRatio = (site.transform.position.y - yMin) / yRange;
                        break;
                    case CutOrientation.Coronal:
                        horizontalRatio = 1 - ((site.transform.position.x - xMin) / xRange);
                        verticalRatio = (site.transform.position.z - zMin) / zRange;
                        break;
                    case CutOrientation.Sagital:
                        horizontalRatio = (site.transform.position.y - yMin) / yRange;
                        verticalRatio = (site.transform.position.z - zMin) / zRange;
                        break;
                }
                if (m_Cut.Flip)
                {
                    horizontalRatio = 1 - horizontalRatio;
                }
                CutSite cutSite = Instantiate(m_SitePrefab, m_SitesRectTransform).GetComponent<CutSite>();
                cutSite.Initialize(m_Scene, site, new Vector2(horizontalRatio, verticalRatio));
            }
        }
        #endregion
    }
}