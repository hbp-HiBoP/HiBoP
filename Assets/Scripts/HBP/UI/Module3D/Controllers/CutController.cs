using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tools.Unity;
using System;

namespace HBP.UI.Module3D
{
    public class CutController : MonoBehaviour
    {
        #region Properties
        private const float MINIMIZED_THRESHOLD = 100.0f;
        /// <summary>
        /// Associated scene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Displayed UI view
        /// </summary>
        private ResizableGrid m_ParentGrid;
        /// <summary>
        /// Recttransform
        /// </summary>
        private RectTransform m_RectTransform;
        /// <summary>
        /// Content of the ScrollView
        /// </summary>
        [SerializeField]
        private RectTransform m_Content;
        /// <summary>
        /// Prefab to control the parameters of a cut
        /// </summary>
        [SerializeField]
        private GameObject m_CutControlPrefab;
        /// <summary>
        /// Reference to the transform of the button used to add a cut
        /// </summary>
        [SerializeField]
        private RectTransform m_AddCutButton;
        /// <summary>
        /// GameObject to hide a minimized column
        /// </summary>
        [SerializeField]
        private GameObject m_MinimizedGameObject;
        private List<CutParametersController> m_CutParametersControllers = new List<CutParametersController>();
        public Tuple<Data.Enums.CutOrientation, Texture2D>[] CutTextures { get { return (from cutParameterController in m_CutParametersControllers select new Tuple<Data.Enums.CutOrientation, Texture2D>(cutParameterController.Cut.Orientation, cutParameterController.Texture)).ToArray(); } }
        
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
        private void AddCut(Cut cut)
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
            m_AddCutButton.SetAsLastSibling();
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_AddCutButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_Scene.AddCutPlane();
                m_CutParametersControllers.Last().OpenControls();
            });

            foreach (Cut cut in m_Scene.Cuts)
            {
                AddCut(cut);
            }
            m_Scene.OnAddCut.AddListener((cut) =>
            {
                AddCut(cut);
            });
        }
        #endregion
    }
}