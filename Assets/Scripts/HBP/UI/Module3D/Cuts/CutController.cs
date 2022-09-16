using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using HBP.Core.Tools;
using HBP.UI.Tools.ResizableGrids;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Base class of the cuts control panel
    /// </summary>
    /// <remarks>
    /// This class allows the addition of new cuts in the scene and the modification of any cut already in the scene
    /// </remarks>
    public class CutController : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Minimum width of the panel before it is considered as minimized
        /// </summary>
        private const float MINIMIZED_THRESHOLD = 100.0f;
        /// <summary>
        /// Associated 3D scene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Parent ResizableGrid in which the panel is
        /// </summary>
        private ResizableGrid m_ParentGrid;
        /// <summary>
        /// RectTransform of this object
        /// </summary>
        private RectTransform m_RectTransform;
        /// <summary>
        /// Content of the ScrollView
        /// </summary>
        [SerializeField] private RectTransform m_Content;
        /// <summary>
        /// Prefab of the object used to control the parameters of a cut
        /// </summary>
        [SerializeField] private GameObject m_CutControlPrefab;
        /// <summary>
        /// Reference to the transform of the button used to add a cut
        /// </summary>
        [SerializeField] private Button m_AddCutButton;
        /// <summary>
        /// GameObject to hide the panel when it is minimized
        /// </summary>
        [SerializeField] private GameObject m_MinimizedGameObject;
        /// <summary>
        /// List of the objects that allow the modification of the cuts
        /// </summary>
        /// <remarks>
        /// There is one CutParametersController for each cut in the scene
        /// </remarks>
        private List<CutParametersController> m_CutParametersControllers = new List<CutParametersController>();
        /// <summary>
        /// Array of Tuples containing the orientation of each cut and its respective generated texture
        /// </summary>
        public Tuple<CutOrientation, Texture2D>[] CutTextures { get { return (from cutParameterController in m_CutParametersControllers select new Tuple<CutOrientation, Texture2D>(cutParameterController.Cut.Orientation, cutParameterController.Texture)).ToArray(); } }
        /// <summary>
        /// Is the panel minimized ?
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ParentGrid.MinimumViewWidth) <= MINIMIZED_THRESHOLD;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ParentGrid = GetComponentInParent<ResizableGrid>();
            m_RectTransform = GetComponent<RectTransform>();
        }
        private void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                m_MinimizedGameObject.SetActive(IsMinimized);
                m_RectTransform.hasChanged = false;
            }
            if (Input.GetMouseButtonDown(0) && m_CutParametersControllers.Any(c => c.AreControlsOpen))
            {
                Rect rect = m_RectTransform.ToScreenSpace();
                Vector3 mousePosition = Input.mousePosition;
                if (!(mousePosition.x >= rect.x && mousePosition.x <= rect.x + rect.width && mousePosition.y >= rect.y && mousePosition.y <= rect.y + rect.height))
                {
                    foreach (CutParametersController control in m_CutParametersControllers)
                    {
                        control.CloseControls();
                    }
                }
            }
        }
        /// <summary>
        /// Add a new cut to the cut controller
        /// </summary>
        /// <param name="cut">Cut that has been added to the scene</param>
        private void AddCut(Core.Object3D.Cut cut)
        {
            CutParametersController controller = Instantiate(m_CutControlPrefab, m_Content).GetComponent<CutParametersController>();
            controller.Initialize(m_Scene, cut);
            m_CutParametersControllers.Add(controller);
            cut.OnRemoveCut.AddListener(() =>
            {
                m_CutParametersControllers.Remove(controller);
            });
            controller.OnOpenControls.AddListener(() =>
            {
                foreach (CutParametersController control in m_CutParametersControllers)
                {
                    if (control != controller)
                    {
                        control.CloseControls();
                    }
                }
            });
            controller.CloseControls();
            controller.Interactable = !m_Scene.AutomaticCutAroundSelectedSite;
            m_AddCutButton.transform.SetAsLastSibling();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the cut controller
        /// </summary>
        /// <param name="scene">Parent scene of the cut controller</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_AddCutButton.onClick.AddListener(() =>
            {
                m_Scene.AddCutPlane();
                m_CutParametersControllers.Last().OpenControls();
            });

            foreach (Core.Object3D.Cut cut in m_Scene.Cuts)
            {
                AddCut(cut);
            }
            m_Scene.OnAddCut.AddListener((cut) =>
            {
                AddCut(cut);
            });
            m_Scene.OnChangeAutomaticCutAroundSelectedSite.AddListener((isOn) =>
            {
                foreach (var controller in m_CutParametersControllers)
                {
                    controller.Interactable = !isOn;
                }
                m_AddCutButton.gameObject.SetActive(!isOn);
            });
        }
        #endregion
    }
}