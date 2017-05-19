using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine.UI;

public class Scene3DUI : MonoBehaviour {
    #region Properties
    /// <summary>
    /// Associated logical Base3DScene
    /// </summary>
    public HBP.Module3D.Base3DScene Scene { get; set; }
    /// <summary>
    /// List of GameObjects to be shown when a column is minimized
    /// </summary>
    private List<GameObject> m_MinimizedColumns = new List<GameObject>();
    /// <summary>
    /// Prefab of the column minimized object
    /// </summary>
    public GameObject MinimizedColumnPrefab;
    #endregion

    #region Private Methods
    private void Awake()
    {
        ApplicationState.Module3D.ScenesManager.OnAddScene.AddListener((scene) =>
        {
            Scene = scene;
            AddColumns();
            GetComponent<ResizableGrid>().AddViewLine();
            for (int i = 0; i < GetComponent<ResizableGrid>().Columns.Count; i++)
            {
                GetComponent<ResizableGrid>().Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(Scene.Column3DViewManager.Columns[i].Views.Last());
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
        for (int i = 0; i < Scene.Column3DViewManager.Columns.Count; i++)
        {
            GetComponent<ResizableGrid>().AddColumn();
            GetComponent<ResizableGrid>().Columns.Last().GetComponent<Column3DUI>().Initialize(Scene.Column3DViewManager.Columns[i]);
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Add a logical and a displayed view
    /// </summary>
    public void AddView()
    {
        for (int i = 0; i < Scene.Column3DViewManager.Columns.Count; i++)
        {
            Scene.Column3DViewManager.Columns[i].AddView();
        }
        GetComponent<ResizableGrid>().AddViewLine();
        for (int i = 0; i < GetComponent<ResizableGrid>().Columns.Count; i++)
        {
            GetComponent<ResizableGrid>().Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(Scene.Column3DViewManager.Columns[i].Views.Last());
        }
    }
    /// <summary>
    /// Remove a logical and displayed view
    /// </summary>
    public void RemoveView()
    {
        for (int i = 0; i < Scene.Column3DViewManager.Columns.Count; i++)
        {
            Scene.Column3DViewManager.Columns[i].RemoveView();
        }
        GetComponent<ResizableGrid>().RemoveViewLine(GetComponent<ResizableGrid>().ViewNumber - 1);
    }
    #endregion
}
