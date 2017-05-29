using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class Scene3DUI : MonoBehaviour {
    #region Properties
    /// <summary>
    /// Associated logical Base3DScene
    /// </summary>
    private HBP.Module3D.Base3DScene m_Scene;
    /// <summary>
    /// List of GameObjects to be shown when a column is minimized
    /// </summary>
    private List<GameObject> m_MinimizedColumns = new List<GameObject>();
    /// <summary>
    /// Linked resizable grid
    /// </summary>
    private ResizableGrid m_ResizableGrid;
    #endregion

    #region Private Methods
    private void Awake()
    {
        m_ResizableGrid = GetComponent<ResizableGrid>();
        ApplicationState.Module3D.ScenesManager.OnAddScene.AddListener((scene) => // TODO : update the methods for N scenes
        {
            m_Scene = scene;
            for (int i = 0; i < m_Scene.ColumnManager.Columns.Count; i++)
            {
                m_ResizableGrid.AddColumn();
                m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene.ColumnManager.Columns[i]);
            }
            m_ResizableGrid.AddViewLine();
            for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
            {
                m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene.ColumnManager.Columns[i].Views.Last());
            }
        });
        ApplicationState.Module3D.OnAddColumn.AddListener(() =>
        {
            if (!m_Scene) return; // if the scene has not been initialized, don't proceed

            m_ResizableGrid.AddColumn();
            m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene.ColumnManager.Columns.Last());
        });
        ApplicationState.Module3D.OnRemoveColumn.AddListener((column) =>
        {

        });
        ApplicationState.Module3D.OnAddViewLine.AddListener(() =>
        {
            m_ResizableGrid.AddViewLine();
            for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
            {
                m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene.ColumnManager.Columns[i].Views.Last());
            }
        });
        ApplicationState.Module3D.OnRemoveViewLine.AddListener((lineID) =>
        {
            m_ResizableGrid.RemoveViewLine(lineID);
        });
    }
    #endregion

    #region Public Methods
    #endregion
}
