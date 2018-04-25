using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using HBP.Module3D;
using Tools.Unity;

namespace HBP.UI.Module3D
{
    public class Scene3DUI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated logical Base3DScene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Linked resizable grid
        /// </summary>
        private ResizableGrid m_ResizableGrid;
        /// <summary>
        /// Update circle when loading things
        /// </summary>
        private LoadingCircle m_LoadingCircle;
        /// <summary>
        /// Feedback for when the iEEG are not up to date
        /// </summary>
        [SerializeField]
        private GameObject m_IEEGOutdated;
        /// <summary>
        /// GameObject to hide a minimized column
        /// </summary>
        [SerializeField]
        private GameObject m_MinimizedGameObject;
        private bool m_RectTransformChanged;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ResizableGrid = GetComponent<ResizableGrid>();
        }
        private void OnDestroy()
        {
            if (m_LoadingCircle)
            {
                m_LoadingCircle.Close();
            }
        }
        private void Update()
        {
            if (m_RectTransformChanged)
            {
                if (GetComponent<RectTransform>().rect.width < 5 * m_ResizableGrid.MinimumViewWidth)
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the UI Scene
        /// </summary>
        /// <param name="scene">Logical associated scene</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            for (int i = 0; i < m_Scene.ColumnManager.Columns.Count; i++)
            {
                m_ResizableGrid.AddColumn();
                m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns[i]);
            }
            m_ResizableGrid.AddViewLine();
            for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
            {
                //m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns[i], m_Scene.ColumnManager.Columns[i].Views.Last());
                Column3DUI columnUI = m_ResizableGrid.Columns[i].GetComponent<Column3DUI>();
                View3DUI viewUI = m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>();
                viewUI.Initialize(m_Scene, m_Scene.ColumnManager.Columns[i], m_Scene.ColumnManager.Columns[i].Views.Last());
                viewUI.OnChangeViewSize.AddListener(() =>
                {
                    columnUI.UpdateLabelPosition();
                });
            }

            // Listeners
            m_Scene.ColumnManager.OnAddColumn.AddListener(() =>
            {
                if (!m_Scene) return; // if the scene has not been initialized, don't proceed

                m_ResizableGrid.AddColumn();
                m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns.Last());
            });
            m_Scene.ColumnManager.OnRemoveColumn.AddListener((column) =>
            {
                if (!m_Scene) return;

                int columnID = m_Scene.ColumnManager.Columns.ToList().FindIndex((c) => c == column);
                m_ResizableGrid.RemoveColumn(m_ResizableGrid.Columns[columnID]);
            });
            m_Scene.ColumnManager.OnAddViewLine.AddListener(() =>
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
                        columnUI.UpdateLabelPosition();
                    });
                }
            });
            m_Scene.ColumnManager.OnRemoveViewLine.AddListener((lineID) =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.RemoveViewLine(lineID);
            });
            m_Scene.OnResetViewPositions.AddListener(() =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.ResetPositions();
            });
            m_Scene.OnUpdatingGenerator.AddListener((value) =>
            {
                if (value)
                {
                    if(m_LoadingCircle == null)
                    {
                        m_LoadingCircle = ApplicationState.LoadingManager.Open();
                        RectTransform loadingCircleRectTransform = m_LoadingCircle.GetComponent<RectTransform>();
                        loadingCircleRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                        loadingCircleRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        loadingCircleRectTransform.anchoredPosition = new Vector2(0f, 0f);
                        m_LoadingCircle.ChangePercentage(0.0f, 99.0f, 1.0f);
                    }        
                }
                else
                {
                    if(m_LoadingCircle != null)
                    {
                        m_LoadingCircle.Close();
                    }
                }
            });
            m_Scene.OnProgressUpdateGenerator.AddListener((progress, duration, message) =>
            {
                if (m_LoadingCircle != null) m_LoadingCircle.ChangePercentage(progress, duration, message);
            });
            m_Scene.OnIEEGOutdated.AddListener((state) =>
            {
                m_IEEGOutdated.SetActive(state);
            });
        }
        public void OnRectTransformDimensionsChange()
        {
            m_RectTransformChanged = true;
        }
        #endregion
    }
}