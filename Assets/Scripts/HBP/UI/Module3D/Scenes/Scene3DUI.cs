using UnityEngine;
using System.Linq;
using Tools.Unity.ResizableGrid;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class is used to properly display a scene 3D in the UI
    /// </summary>
    public class Scene3DUI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// If the difference between the width of the scene and the default minimum width of a column in the ResizableGrid is less than this value, it is considered minimized
        /// </summary>
        private const float MINIMIZED_THRESHOLD = 200.0f;
        /// <summary>
        /// Associated logical Base3DScene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Linked resizable grid
        /// </summary>
        private ResizableGrid m_ResizableGrid;
        /// <summary>
        /// Reference to the RectTransform of this object
        /// </summary>
        private RectTransform m_RectTransform;

        /// <summary>
        /// Progress bar overlay element to show feedback when computing the activity on the brain
        /// </summary>
        [SerializeField] private ProgressBar m_ProgressBar;
        /// <summary>
        /// Feedback for when the iEEG are not up to date
        /// </summary>
        [SerializeField] private IEEGOutdated m_IEEGOutdated;
        /// <summary>
        /// GameObject to hide a minimized scene
        /// </summary>
        [SerializeField] private GameObject m_MinimizedGameObject;
        /// <summary>
        /// Is the scene minimzed ?
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ResizableGrid.MinimumViewWidth) <= MINIMIZED_THRESHOLD;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_ResizableGrid = GetComponent<ResizableGrid>();
        }
        private void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                m_MinimizedGameObject.SetActive(IsMinimized);
                m_RectTransform.hasChanged = false;
            }

            if (m_Scene.UpdatingGenerators)
            {
                m_ProgressBar.Open();
            }
            else
            {
                m_ProgressBar.Close();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the UI Scene
        /// </summary>
        /// <param name="scene">Logical associated scene</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_IEEGOutdated.Initialize();
            for (int i = 0; i < m_Scene.Columns.Count; i++)
            {
                m_ResizableGrid.AddColumn();
                Column3DUI columnUI = m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>();
                columnUI.Initialize(m_Scene, m_Scene.Columns[i]);
                columnUI.OnChangeColumnSize.AddListener(() =>
                {
                    columnUI.UpdateOverlayElementsPosition();
                    UpdateOverlayElementsPosition();
                });
            }
            m_ResizableGrid.AddViewLine();
            for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
            {
                Column3DUI columnUI = m_ResizableGrid.Columns[i].GetComponent<Column3DUI>();
                View3DUI viewUI = m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>();
                viewUI.Initialize(m_Scene, m_Scene.Columns[i], m_Scene.Columns[i].Views.Last());
                viewUI.OnChangeViewSize.AddListener(() =>
                {
                    columnUI.UpdateOverlayElementsPosition();
                    UpdateOverlayElementsPosition();
                });
            }

            // Listeners
            m_Scene.OnAddColumn.AddListener(() =>
            {
                if (!m_Scene) return; // if the scene has not been initialized, don't proceed

                m_ResizableGrid.AddColumn();
                m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene, m_Scene.Columns.Last());
            });
            m_Scene.OnAddViewLine.AddListener(() =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.AddViewLine();
                for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
                {
                    //m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene, m_ResizableGrid.Columns[i].GetComponent<Column3DUI>().Column, m_ResizableGrid.Columns[i].GetComponent<Column3DUI>().Column.Views.Last());
                    Column3DUI columnUI = m_ResizableGrid.Columns[i].GetComponent<Column3DUI>();
                    View3DUI viewUI = m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>();
                    viewUI.Initialize(m_Scene, columnUI.Column, columnUI.Column.Views.Last());
                    viewUI.OnChangeViewSize.AddListener(() =>
                    {
                        columnUI.UpdateOverlayElementsPosition();
                        UpdateOverlayElementsPosition();
                    });
                }
            });
            m_Scene.OnRemoveViewLine.AddListener((lineID) =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.RemoveViewLine(lineID);
            });
            m_Scene.OnResetViewPositions.AddListener(() =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.ResetPositions();
            });
            m_Scene.OnProgressUpdateGenerator.AddListener((progress, message, duration) =>
            {
                m_ProgressBar.Progress(progress, message, duration);
            });
            m_Scene.OnIEEGOutdated.AddListener((state) =>
            {
                m_IEEGOutdated.gameObject.SetActive(state);
            });
        }
        /// <summary>
        /// Update the position of the overlay
        /// </summary>
        public void UpdateOverlayElementsPosition()
        {
            if (m_ResizableGrid.ColumnNumber > 0)
            {
                // Vertical
                Column gridColumn = m_ResizableGrid.Columns[0];
                float verticalOffset = 0.0f;
                for (int i = gridColumn.Views.Count - 1; i >= 0; --i)
                {
                    View3DUI view = gridColumn.Views[i].GetComponent<View3DUI>();
                    if (view.IsViewMinimizedAndColumnNotMinimized)
                    {
                        verticalOffset += view.GetComponent<RectTransform>().rect.height;
                    }
                    else
                    {
                        break;
                    }
                }
                m_IEEGOutdated.SetVerticalOffset(verticalOffset);

                // Horizontal
                float horizontalOffset = 0.0f;
                for (int i = m_ResizableGrid.Columns.Count - 1; i >= 0; --i)
                {
                    Column3DUI column = m_ResizableGrid.Columns[i].GetComponent<Column3DUI>();
                    if (column.IsMinimized)
                    {
                        horizontalOffset -= column.GetComponent<RectTransform>().rect.width;
                    }
                    else
                    {
                        break;
                    }
                }
                m_IEEGOutdated.SetHorizontalOffset(horizontalOffset);
            }
        }
        #endregion
    }
}