using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Column3DUI : MonoBehaviour {
    #region Properties
    private Column3DView m_Column;
    /// <summary>
    /// Associated logical column 3D
    /// </summary>
    public Column3DView Column
    {
        get
        {
            return m_Column;
        }
    }
    /// <summary>
    /// Parent resizable grid
    /// </summary>
    public ResizableGrid ParentGrid { get; set; }
    /// <summary>
    /// Is the column initialized ?
    /// </summary>
    private bool m_IsInitialized = false;
    /// <summary>
    /// Event called when resizing a column
    /// </summary>
    public UnityEvent OnColumnResize = new UnityEvent();
    #endregion

    #region Private Methods
    private void Awake()
    {
        ParentGrid = GetComponentInParent<ResizableGrid>();
    }
    /// <summary>
    /// Get RectTransform screen coordinates
    /// </summary>
    /// <param name="transform">Rect Transform to get screen coordinates from</param>
    /// <returns></returns>
    private Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }
    #endregion

    #region Public Methods
    public void OnRectTransformDimensionsChange()
    {
        if (!m_IsInitialized) return;

        OnColumnResize.Invoke();
    }
    /// <summary>
    /// Initialize this view
    /// </summary>
    public void Initialize(Column3DView column)
    {
        m_Column = column;
        m_IsInitialized = true;
    }
    #endregion
}
