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
        ApplicationState.Module3D.ScenesManager.OnAddScene.AddListener((scene) =>
        {
            m_Scene = scene;
            AddColumns();
            m_ResizableGrid.AddViewLine();
            for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
            {
                m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene.ColumnManager.Columns[i].Views.Last());
            }
        });
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AddView();
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            RemoveView();
        }
    }
    /// <summary>
    /// Add displayed columns but no logic behind
    /// </summary>
    private void AddColumns()
    {
        for (int i = 0; i < m_Scene.ColumnManager.Columns.Count; i++)
        {
            m_ResizableGrid.AddColumn();
            m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene.ColumnManager.Columns[i]);
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Add a logical and a displayed view
    /// </summary>
    public void AddView()
    {
        for (int i = 0; i < m_Scene.ColumnManager.Columns.Count; i++)
        {
            m_Scene.ColumnManager.Columns[i].AddView();
        }
        m_ResizableGrid.AddViewLine();
        for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
        {
            m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene.ColumnManager.Columns[i].Views.Last());
        }
    }
    /// <summary>
    /// Remove a logical and displayed view
    /// </summary>
    public void RemoveView()
    {
        for (int i = 0; i < m_Scene.ColumnManager.Columns.Count; i++)
        {
            m_Scene.ColumnManager.Columns[i].RemoveView();
        }
        m_ResizableGrid.RemoveViewLine(m_ResizableGrid.ViewNumber - 1);
    }
    #endregion
}
