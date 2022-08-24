using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools.ResizableGrids;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class is used to properly display a column 3D in the UI
    /// </summary>
    public class Column3DUI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Minimum width of the column 3D ui in order to have enough space to display the overlay elements
        /// </summary>
        private const int MINIMUM_WIDTH_TO_DISPLAY_OVERLAY = 200;
        /// <summary>
        /// If the difference between the width of the column and the default minimum width of a column in the ResizableGrid is less than this value, it is considered minimized
        /// </summary>
        private const float MINIMIZED_THRESHOLD = 10.0f;
        /// <summary>
        /// Associated logical Column3D
        /// </summary>
        public Column3D Column { get; private set; }

        /// <summary>
        /// Corresponding column of the ResizableGrid
        /// </summary>
        private Column m_GridColumn;
        /// <summary>
        /// Parent resizable grid
        /// </summary>
        public ResizableGrid ParentGrid { get; private set; }
        /// <summary>
        /// Reference to this object's RectTransform
        /// </summary>
        [SerializeField] private RectTransform m_RectTransform;
        /// <summary>
        /// GameObject to hide a minimized column
        /// </summary>
        [SerializeField] private GameObject m_MinimizedGameObject;
        /// <summary>
        /// Associated label overlay element
        /// </summary>
        [SerializeField] private ColumnLabel m_Label;
        /// <summary>
        /// Associated colormap overlay element
        /// </summary>
        [SerializeField] private Colormap m_Colormap;
        /// <summary>
        /// Associated colormap overlay element
        /// </summary>
        public Colormap Colormap { get { return m_Colormap; } }
        /// <summary>
        /// Associated timeline overlay element
        /// </summary>
        [SerializeField] private TimeDisplay m_TimeDisplay;
        /// <summary>
        /// Associated Icon overlay element
        /// </summary>
        [SerializeField] private Icon m_Icon;
        /// <summary>
        /// Associated Icon overlay element
        /// </summary>
        public Icon Icon { get { return m_Icon; } }
        /// <summary>
        /// Associated information overlay element
        /// </summary>
        [SerializeField] private ColumnInformation m_Information;
        /// <summary>
        /// Associated resizer overlay element
        /// </summary>
        [SerializeField] private ColumnResizer m_Resizer;
        /// <summary>
        /// List of all the views of this column UI
        /// </summary>
        public List<View3DUI> Views { get; private set; } = new List<View3DUI>();
        
        /// <summary>
        /// Used when drag and dropping a column onto this columnn
        /// </summary>
        [SerializeField] private RectTransform m_Middle;
        /// <summary>
        /// Used when drag and dropping a column onto the left part of this columnn
        /// </summary>
        [SerializeField] private RectTransform m_LeftBorder;
        /// <summary>
        /// Used when drag and dropping a column onto the right part of this columnn
        /// </summary>
        [SerializeField] private RectTransform m_RightBorder;


        /// <summary>
        /// Does the column UI have enough space to display the overlay ?
        /// </summary>
        public bool HasEnoughSpaceForOverlay
        {
            get
            {
                return m_RectTransform.rect.width > MINIMUM_WIDTH_TO_DISPLAY_OVERLAY;
            }
        }
        /// <summary>
        /// Is the column minimzed ?
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - ParentGrid.MinimumViewWidth) <= MINIMIZED_THRESHOLD;
            }
        }

        /// <summary>
        /// Is the mouse currently over this column ?
        /// </summary>
        public bool IsHovered
        {
            get
            {
                Vector3 mousePosition = Input.mousePosition;
                Rect columnRect = GetComponent<RectTransform>().ToScreenSpace();
                return mousePosition.x >= columnRect.x && mousePosition.x <= columnRect.x + columnRect.width && mousePosition.y >= columnRect.y && mousePosition.y <= columnRect.y + columnRect.height;
            }
        }
        /// <summary>
        /// Is the mouse currently over the left part of this column ?
        /// </summary>
        public bool IsLeftBorderHovered
        {
            get
            {
                Vector3 mousePosition = Input.mousePosition;
                Rect borderRect = m_LeftBorder.ToScreenSpace();
                return mousePosition.x >= borderRect.x && mousePosition.x <= borderRect.x + borderRect.width && mousePosition.y >= borderRect.y && mousePosition.y <= borderRect.y + borderRect.height;
            }
        }
        /// <summary>
        /// Is the mouse currently over the right part of this column ?
        /// </summary>
        public bool IsRightBorderHovered
        {
            get
            {
                Vector3 mousePosition = Input.mousePosition;
                Rect borderRect = m_RightBorder.ToScreenSpace();
                return mousePosition.x >= borderRect.x && mousePosition.x <= borderRect.x + borderRect.width && mousePosition.y >= borderRect.y && mousePosition.y <= borderRect.y + borderRect.height;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing the size of the column (on the update after the change)
        /// </summary>
        public UnityEvent OnChangeColumnSize = new UnityEvent();
        #endregion

        #region Private Methods
        private void Awake()
        {
            ParentGrid = GetComponentInParent<ResizableGrid>();
            m_GridColumn = GetComponent<Column>();
        }
        private void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                UpdateOverlay();
                m_MinimizedGameObject.SetActive(IsMinimized);
                Column.IsMinimized = IsMinimized;
                OnChangeColumnSize.Invoke();
                m_RectTransform.hasChanged = false;
            }
        }
        /// <summary>
        /// Hide or display the overlay elements depending on the available space
        /// </summary>
        private void UpdateOverlay()
        {
            m_Colormap.HandleEnoughSpace();
            m_TimeDisplay.HandleEnoughSpace();
            m_Icon.HandleEnoughSpace();
            m_Information.HandleEnoughSpace();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the column UI
        /// </summary>
        /// <param name="scene">Parent scene of the corresponding Column3D</param>
        /// <param name="column">Corresponding Column3D</param>
        public void Initialize(Base3DScene scene, Column3D column)
        {
            Column = column;

            m_MinimizedGameObject.GetComponentInChildren<Text>().text = Column.Name;
            m_MinimizedGameObject.SetActive(false);

            m_Colormap.Setup(scene, column, this);
            m_TimeDisplay.Setup(scene, column, this);
            m_Icon.Setup(scene, column, this);
            m_Information.Setup(scene, column, this);
            m_Label.Setup(scene, column, this);
            m_Resizer.Setup(scene, column, this);
        }
        /// <summary>
        /// Expand this column (set the width becomes the maximum possible)
        /// </summary>
        public void Expand()
        {
            int id = ParentGrid.Columns.IndexOf(m_GridColumn);
            float minimizedWidth = (ParentGrid.MinimumViewWidth / ParentGrid.RectTransform.rect.width);
            if (IsMinimized)
            {
                float availableWidth = 1.0f;
                int numberOfExpandedColumns = 0;
                for (int i = 0; i < ParentGrid.Columns.Count; i++)
                {
                    Column3DUI col = ParentGrid.Columns[i].GetComponent<Column3DUI>();
                    if (!col.IsMinimized || col == this)
                    {
                        numberOfExpandedColumns++;
                    }
                    else
                    {
                        availableWidth -= minimizedWidth;
                    }
                }
                float width = availableWidth / numberOfExpandedColumns;
                for (int i = 0; i < ParentGrid.Columns.Count - 1; i++)
                {
                    Column3DUI col = ParentGrid.Columns[i].GetComponent<Column3DUI>();
                    if (!col.IsMinimized || col == this)
                    {
                        if (i == 0)
                        {
                            ParentGrid.VerticalHandlers[i].Position = width;
                        }
                        else
                        {
                            ParentGrid.VerticalHandlers[i].Position = ParentGrid.VerticalHandlers[i - 1].Position + width;
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            ParentGrid.VerticalHandlers[i].Position = minimizedWidth;
                        }
                        else
                        {
                            ParentGrid.VerticalHandlers[i].Position = ParentGrid.VerticalHandlers[i - 1].Position + minimizedWidth;
                        }
                    }
                    ParentGrid.SetVerticalHandlersPosition(i);
                }
                ParentGrid.UpdateAnchors();
            }
            else
            {
                if (id != 0)
                {
                    ParentGrid.VerticalHandlers[id - 1].Position = 0.0f;
                    ParentGrid.SetVerticalHandlersPosition(id - 1);
                }
                if (id != ParentGrid.Columns.Count - 1)
                {
                    ParentGrid.VerticalHandlers[id].Position = 1.0f;
                    ParentGrid.SetVerticalHandlersPosition(id);
                }
                ParentGrid.UpdateAnchors();
            }
        }
        /// <summary>
        /// Minimize this column (set the width to the minimum possible and put the column on the right)
        /// </summary>
        public void Minimize()
        {
            if (IsMinimized) return;
            
            int id = ParentGrid.Columns.IndexOf(m_GridColumn);
            float minimizedWidth = (ParentGrid.MinimumViewWidth / ParentGrid.RectTransform.rect.width);

            float totalWidth = 0.0f, availableWidth = 1.0f;
            List<float> widths = new List<float>();
            for (int i = 0; i < ParentGrid.Columns.Count; i++)
            {
                Column3DUI col = ParentGrid.Columns[i].GetComponent<Column3DUI>();
                if (!col.IsMinimized && col != this)
                {
                    if (i == 0)
                    {
                        totalWidth += ParentGrid.VerticalHandlers[i].Position;
                        widths.Add(ParentGrid.VerticalHandlers[i].Position);
                    }
                    else if (i == ParentGrid.VerticalHandlers.Count)
                    {
                        totalWidth += (1.0f - ParentGrid.VerticalHandlers[i - 1].Position);
                        widths.Add(1.0f - ParentGrid.VerticalHandlers[i - 1].Position);
                    }
                    else
                    {
                        totalWidth += (ParentGrid.VerticalHandlers[i].Position - ParentGrid.VerticalHandlers[i - 1].Position);
                        widths.Add(ParentGrid.VerticalHandlers[i].Position - ParentGrid.VerticalHandlers[i - 1].Position);
                    }
                }
                else
                {
                    availableWidth -= minimizedWidth;
                }
            }
            for (int i = 0; i < widths.Count; i++)
            {
                widths[i] /= totalWidth;
                widths[i] *= availableWidth;
            }

            while (true)
            {
                id = ParentGrid.Columns.IndexOf(m_GridColumn);
                if (id == ParentGrid.Columns.Count - 1) break;

                ParentGrid.SwapColumns(ParentGrid.Columns[id], ParentGrid.Columns[id + 1]);
            }

            int widthIndex = 0;
            for (int i = 0; i < ParentGrid.Columns.Count - 1; i++)
            {
                Column3DUI col = ParentGrid.Columns[i].GetComponent<Column3DUI>();
                if (!col.IsMinimized && col != this)
                {
                    if (i == 0)
                    {
                        ParentGrid.VerticalHandlers[i].Position = widths[widthIndex];
                    }
                    else
                    {
                        ParentGrid.VerticalHandlers[i].Position = ParentGrid.VerticalHandlers[i - 1].Position + widths[widthIndex];
                    }
                    widthIndex++;
                }
                else
                {
                    if (i == 0)
                    {
                        ParentGrid.VerticalHandlers[i].Position = minimizedWidth;
                    }
                    else
                    {
                        ParentGrid.VerticalHandlers[i].Position = ParentGrid.VerticalHandlers[i - 1].Position + minimizedWidth;
                    }
                }
                ParentGrid.SetVerticalHandlersPosition(i);
            }
            ParentGrid.UpdateAnchors();
        }
        /// <summary>
        /// Move the column in a specific direction by a specific amount
        /// </summary>
        /// <param name="direction">Direction and amount to move the column (e.g. +2 will move this column to the right by 2 columns)</param>
        public void Move(int direction)
        {
            int id = ParentGrid.Columns.IndexOf(m_GridColumn);
            int goalID = id + direction;

            while (true)
            {
                id = ParentGrid.Columns.IndexOf(m_GridColumn);
                if ((id == ParentGrid.Columns.Count - 1 && direction > 0) || (id == 0 && direction < 0) || id == goalID) break;

                ParentGrid.SwapColumns(ParentGrid.Columns[id], ParentGrid.Columns[id + (int)Mathf.Sign(direction) * 1]);
            }
        }
        /// <summary>
        /// Swap this column with the hovered column
        /// </summary>
        public void SwapColumnWithHoveredColumn()
        {
            foreach (Column column in ParentGrid.Columns)
            {
                Column3DUI columnUI = column.GetComponent<Column3DUI>();
                if (columnUI.IsHovered)
                {
                    if (columnUI.IsLeftBorderHovered)
                    {
                        int id1 = ParentGrid.Columns.IndexOf(m_GridColumn);
                        int id2 = ParentGrid.Columns.IndexOf(column);
                        if (id1 > id2)
                        {
                            Move(id2 - id1);
                        }
                        else if (id2 > id1)
                        {
                            Move(id2 - id1 - 1);
                        }
                    }
                    else if (columnUI.IsRightBorderHovered)
                    {
                        int id1 = ParentGrid.Columns.IndexOf(m_GridColumn);
                        int id2 = ParentGrid.Columns.IndexOf(column);
                        if (id1 > id2)
                        {
                            Move(id2 - id1 + 1);
                        }
                        else if (id2 > id1)
                        {
                            Move(id2 - id1);
                        }
                    }
                    else
                    {
                        ParentGrid.SwapColumns(m_GridColumn, column);
                    }
                    return;
                }
            }
        }
        /// <summary>
        /// Update the visibility of the border depending on the position of the cursor
        /// </summary>
        /// <param name="forceInactive">Disable all borders</param>
        public void UpdateBorderVisibility(bool forceInactive = false)
        {
            UnityEngine.Profiling.Profiler.BeginSample("border visibility");
            int id = ParentGrid.Columns.IndexOf(m_GridColumn);
            if (forceInactive)
            {
                foreach (Column column in ParentGrid.Columns)
                {
                    Column3DUI columnUI = column.GetComponent<Column3DUI>();
                    columnUI.m_Middle.gameObject.SetActive(false);
                    columnUI.m_LeftBorder.gameObject.SetActive(false);
                    columnUI.m_RightBorder.gameObject.SetActive(false);
                }
            }
            else
            {
                foreach (Column column in ParentGrid.Columns)
                {
                    Column3DUI columnUI = column.GetComponent<Column3DUI>();
                    int columnID = ParentGrid.Columns.IndexOf(column);
                    if (columnUI == this) continue;

                    bool columnHovered = columnUI.IsHovered;
                    bool leftHovered = columnUI.IsLeftBorderHovered;
                    bool rightHovered = columnUI.IsRightBorderHovered;
                    if ((rightHovered || leftHovered))
                    {
                        columnUI.m_Middle.gameObject.SetActive(false);
                        columnUI.m_LeftBorder.gameObject.SetActive(leftHovered && id != columnID - 1);
                        columnUI.m_RightBorder.gameObject.SetActive(rightHovered && id != columnID + 1);
                    }
                    else if (columnUI.IsHovered)
                    {
                        columnUI.m_Middle.gameObject.SetActive(true);
                        columnUI.m_LeftBorder.gameObject.SetActive(false);
                        columnUI.m_RightBorder.gameObject.SetActive(false);
                    }
                    else
                    {
                        columnUI.m_Middle.gameObject.SetActive(false);
                        columnUI.m_LeftBorder.gameObject.SetActive(false);
                        columnUI.m_RightBorder.gameObject.SetActive(false);
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the position of the overlay
        /// </summary>
        public void UpdateOverlayElementsPosition()
        {
            // Top
            float topOffset = 0.0f;
            for (int i = 0; i < m_GridColumn.Views.Count; ++i)
            {
                View3DUI view = m_GridColumn.Views[i].GetComponent<View3DUI>();
                if (view.IsViewMinimizedAndColumnNotMinimized)
                {
                    topOffset -= view.GetComponent<RectTransform>().rect.height;
                }
                else
                {
                    break;
                }
            }
            m_Label.SetVerticalOffset(topOffset);
            m_Colormap.SetVerticalOffset(topOffset);
            m_TimeDisplay.SetVerticalOffset(topOffset);
            m_Icon.SetVerticalOffset(topOffset);
            m_Resizer.SetVerticalOffset(topOffset);

            // Bottom
            float bottomOffset = 0.0f;
            for (int i = m_GridColumn.Views.Count - 1; i >= 0; --i)
            {
                View3DUI view = m_GridColumn.Views[i].GetComponent<View3DUI>();
                if (view.IsViewMinimizedAndColumnNotMinimized)
                {
                    bottomOffset += view.GetComponent<RectTransform>().rect.height;
                }
                else
                {
                    break;
                }
            }
            m_Information.SetVerticalOffset(bottomOffset);
        }
        #endregion
    }
}