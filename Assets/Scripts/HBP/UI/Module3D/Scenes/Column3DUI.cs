using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class Column3DUI : MonoBehaviour
    {
        #region Properties
        private const int MINIMUM_SIZE_TO_DISPLAY_OVERLAY = 200;
        private Column3D m_Column;
        /// <summary>
        /// Associated logical column 3D
        /// </summary>
        public Column3D Column
        {
            get
            {
                return m_Column;
            }
        }
        private ResizableGrid m_ParentGrid;
        /// <summary>
        /// Parent resizable grid
        /// </summary>
        public ResizableGrid ParentGrid
        {
            get { return m_ParentGrid; }
        }
        /// <summary>
        /// Reference to this object's RectTransform
        /// </summary>
        [SerializeField]
        private RectTransform m_RectTransform;
        /// <summary>
        /// GameObject to hide a minimized column
        /// </summary>
        [SerializeField]
        private GameObject m_MinimizedGameObject;
        /// <summary>
        /// Associated label
        /// </summary>
        [SerializeField]
        private ColumnLabel m_Label;
        /// <summary>
        /// Associated colormap
        /// </summary>
        [SerializeField]
        private Colormap m_Colormap;
        /// <summary>
        /// Associated timeline
        /// </summary>
        [SerializeField]
        private TimeDisplay m_TimeDisplay;
        /// <summary>
        /// Associated Icon
        /// </summary>
        [SerializeField]
        private Icon m_Icon;
        /// <summary>
        /// Column resizer
        /// </summary>
        [SerializeField]
        private ColumnResizer m_Resizer;
        /// <summary>
        /// Is the column initialized ?
        /// </summary>
        private bool m_IsInitialized = false;
        /// <summary>
        /// Does the column UI have enough space to display the overlay ?
        /// </summary>
        public bool HasEnoughSpaceForOverlay
        {
            get
            {
                return m_RectTransform.rect.width > MINIMUM_SIZE_TO_DISPLAY_OVERLAY;
            }
        }

        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ParentGrid.MinimumViewWidth) <= 0.9f;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ParentGrid = GetComponentInParent<ResizableGrid>();
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
        private void UpdateOverlay()
        {
            m_Label.IsActive = m_Label.IsActive;
            m_Colormap.IsActive = m_Colormap.IsActive;
            m_TimeDisplay.IsActive = m_TimeDisplay.IsActive;
            m_Icon.IsActive = m_Icon.IsActive;
        }
        #endregion

        #region Public Methods
        public void OnRectTransformDimensionsChange()
        {
            if (!m_IsInitialized) return;

            UpdateOverlay();
            m_MinimizedGameObject.SetActive(IsMinimized);
        }
        /// <summary>
        /// Initialize this column UI
        /// </summary>
        public void Initialize(Base3DScene scene, Column3D column)
        {
            m_Column = column;
            m_MinimizedGameObject = transform.Find("MinimizedImage").gameObject;
            m_MinimizedGameObject.GetComponentInChildren<Text>().text = m_Column.Label;
            m_MinimizedGameObject.SetActive(false);
            m_Colormap.Initialize(scene, column, this);
            m_TimeDisplay.Initialize(scene, column, this);
            m_Icon.Initialize(scene, column, this);
            m_Label.Initialize(scene, column, this);
            m_Resizer.Initialize(this);
            m_IsInitialized = true;
        }
        /// <summary>
        /// Expand this column
        /// </summary>
        public void Expand()
        {
            // TODO : change behaviour if column is already minimized
            Column column = GetComponent<Column>();
            int id = m_ParentGrid.Columns.IndexOf(column);
            if (IsMinimized)
            {

            }
            else
            {
                if (id != 0)
                {
                    m_ParentGrid.VerticalHandlers[id - 1].Position = 0.0f;
                    m_ParentGrid.SetVerticalHandlersPosition(id - 1);
                }
                if (id != m_ParentGrid.Columns.Count - 1)
                {
                    m_ParentGrid.VerticalHandlers[id].Position = 1.0f;
                    m_ParentGrid.SetVerticalHandlersPosition(id);
                }
                m_ParentGrid.UpdateAnchors();
            }
        }
        /// <summary>
        /// Minimize this column
        /// </summary>
        public void Minimize()
        {
            if (IsMinimized) return;

            Column column = GetComponent<Column>();
            int id = m_ParentGrid.Columns.IndexOf(column);

            float totalWidth = 0.0f, availableWidth = 1.0f;
            List<float> widths = new List<float>();
            for (int i = 0; i < m_ParentGrid.Columns.Count; i++)
            {
                Column3DUI col = m_ParentGrid.Columns[i].GetComponent<Column3DUI>();
                if (!col.IsMinimized && col != this)
                {
                    if (i == 0)
                    {
                        totalWidth += m_ParentGrid.VerticalHandlers[i].Position;
                        widths.Add(m_ParentGrid.VerticalHandlers[i].Position);
                    }
                    else if (i == m_ParentGrid.VerticalHandlers.Count)
                    {
                        totalWidth += (1.0f - m_ParentGrid.VerticalHandlers[i - 1].Position);
                        widths.Add(1.0f - m_ParentGrid.VerticalHandlers[i - 1].Position);
                    }
                    else
                    {
                        totalWidth += (m_ParentGrid.VerticalHandlers[i].Position - m_ParentGrid.VerticalHandlers[i - 1].Position);
                        widths.Add(m_ParentGrid.VerticalHandlers[i].Position - m_ParentGrid.VerticalHandlers[i - 1].Position);
                    }
                }
                else
                {
                    availableWidth -= (m_ParentGrid.MinimumViewWidth / m_ParentGrid.GetComponent<RectTransform>().rect.width);
                }
            }
            for (int i = 0; i < widths.Count; i++)
            {
                widths[i] /= totalWidth;
                widths[i] *= availableWidth;
            }

            while (true)
            {
                id = m_ParentGrid.Columns.IndexOf(column);
                if (id == m_ParentGrid.Columns.Count - 1) break;

                m_ParentGrid.SwapColumns(m_ParentGrid.Columns[id], m_ParentGrid.Columns[id + 1]);
            }

            int widthIndex = 0;
            for (int i = 0; i < m_ParentGrid.Columns.Count - 1; i++)
            {
                Column3DUI col = m_ParentGrid.Columns[i].GetComponent<Column3DUI>();
                if (!col.IsMinimized && col != this)
                {
                    if (i == 0)
                    {
                        m_ParentGrid.VerticalHandlers[i].Position = widths[widthIndex];
                    }
                    else
                    {
                        m_ParentGrid.VerticalHandlers[i].Position = m_ParentGrid.VerticalHandlers[i - 1].Position + widths[widthIndex];
                    }
                    widthIndex++;
                }
                m_ParentGrid.SetVerticalHandlersPosition(i);
            }
            m_ParentGrid.UpdateAnchors();
        }
        /// <summary>
        /// Move the column in a specific direction
        /// </summary>
        /// <param name="direction"></param>
        public void Move(int direction)
        {
            Column column = GetComponent<Column>();
            int id = m_ParentGrid.Columns.IndexOf(column);
            int goalID = id + direction;

            while (true)
            {
                id = m_ParentGrid.Columns.IndexOf(column);
                if ((id == m_ParentGrid.Columns.Count - 1 && direction > 0) || (id == 0 && direction < 0) || id == goalID) break;

                m_ParentGrid.SwapColumns(m_ParentGrid.Columns[id], m_ParentGrid.Columns[id + (int)Mathf.Sign(direction) * 1]);
            }
        }
        /// <summary>
        /// Swap this column with the hovered column
        /// </summary>
        public void SwapColumnWithHoveredColumn()
        {
            Vector3 mousePosition = Input.mousePosition;
            foreach (Column column in m_ParentGrid.Columns)
            {
                Rect columnRect = RectTransformToScreenSpace(column.GetComponent<RectTransform>());
                if (mousePosition.x >= columnRect.x && mousePosition.x <= columnRect.x + columnRect.width &&
                    mousePosition.y >= columnRect.y && mousePosition.y <= columnRect.y + columnRect.height)
                {
                    m_ParentGrid.SwapColumns(GetComponent<Column>(), column);
                    return;
                }
            }
        }
        #endregion
    }
}