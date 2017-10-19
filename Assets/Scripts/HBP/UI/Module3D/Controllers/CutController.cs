using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class CutController : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated scene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Displayed UI view
        /// </summary>
        private ResizableGrid m_ParentGrid;
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
        private bool m_RectTransformChanged;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ParentGrid = GetComponentInParent<ResizableGrid>();
        }
        private void Update()
        {
            if (m_RectTransformChanged)
            {
                if (GetComponent<RectTransform>().rect.width < 3 * m_ParentGrid.MinimumViewWidth)
                {
                    m_MinimizedGameObject.SetActive(true);
                }
                else
                {
                    m_MinimizedGameObject.SetActive(false);
                }
                m_RectTransformChanged = false;
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
                if (!m_Scene.CuttingEdge) m_Scene.CuttingEdge = true;
            });
            controller.OnCloseControls.AddListener(() =>
            {
                if (m_CutParametersControllers.All(c => !c.AreControlsOpen))
                {
                    m_Scene.CuttingEdge = false;
                }
            });
            controller.CloseControls();
            m_AddCutButton.SetAsLastSibling();
        }
        #endregion

        #region Public Methods
        public void OnRectTransformDimensionsChange()
        {
            m_RectTransformChanged = true;
        }
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