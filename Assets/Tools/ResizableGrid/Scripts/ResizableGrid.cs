using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

namespace Tools.Unity.ResizableGrid
{
    public class ResizableGrid : MonoBehaviour
    {
        #region Properties
        private List<Column> m_Columns = new List<Column>();
        /// <summary>
        /// Columns of the layout
        /// </summary>
        public ReadOnlyCollection<Column> Columns
        {
            get
            {
                return new ReadOnlyCollection<Column>(m_Columns);
            }
        }

        private List<VerticalHandler> m_VerticalHandlers = new List<VerticalHandler>();
        /// <summary>
        /// Vertical handlers of the layout (to resize the columns)
        /// </summary>
        public ReadOnlyCollection<VerticalHandler> VerticalHandlers
        {
            get
            {
                return new ReadOnlyCollection<VerticalHandler>(m_VerticalHandlers);
            }
        }
        
        private List<HorizontalHandler> m_HorizontalHandlers = new List<HorizontalHandler>();
        /// <summary>
        /// Horizontal handlers of the layout (to resize the views)
        /// </summary>
        public ReadOnlyCollection<HorizontalHandler> HorizontalHandlers
        {
            get
            {
                return new ReadOnlyCollection<HorizontalHandler>(m_HorizontalHandlers);
            }
        }

        /// <summary>
        /// Corner handlers of the layout (to resize both the views and the columns)
        /// </summary>
        private List<List<CornerHandler>> m_CornerHandlers = new List<List<CornerHandler>>();

        private const float MINIMUM_VIEW_HEIGHT_DEFAULT = 50.0f;
        private float m_MinimumViewHeight = MINIMUM_VIEW_HEIGHT_DEFAULT;
        /// <summary>
        /// Minimum height of a view
        /// </summary>
        public float MinimumViewHeight
        {
            get
            {
                return m_MinimumViewHeight;
            }
        }

        private const float MINIMUM_VIEW_WIDTH_DEFAULT = 50.0f;
        private float m_MinimumViewWidth = MINIMUM_VIEW_WIDTH_DEFAULT;
        /// <summary>
        /// Minimum width of a view
        /// </summary>
        public float MinimumViewWidth
        {
            get
            {
                return m_MinimumViewWidth;
            }
        }

        /// <summary>
        /// Number of columns in the layout
        /// </summary>
        public int ColumnNumber
        {
            get
            {
                return m_Columns.Count;
            }
        }
        /// <summary>
        /// Maximum number of views of a column in the layout
        /// </summary>
        public int ViewNumber
        {
            get
            {
                int maxViewNumber = 0;
                foreach (Column column in m_Columns)
                {
                    if (column.ViewNumber > maxViewNumber)
                    {
                        maxViewNumber = column.ViewNumber;
                    }
                }
                return maxViewNumber;
            }
        }
        /// <summary>
        /// Number of vertical handlers in the layout
        /// </summary>
        public int VerticalHandlerNumber
        {
            get
            {
                return m_VerticalHandlers.Count;
            }
        }
        /// <summary>
        /// Number of horizontal handlers in the layout
        /// </summary>
        public int HorizontalHandlerNumber
        {
            get
            {
                return m_HorizontalHandlers.Count;
            }
        }
        /// <summary>
        /// Number of corner handlers in the layout
        /// </summary>
        public int CornerHandlerNumber
        {
            get
            {
                int totalSize = 0;
                foreach (List<CornerHandler> columnCornerHandler in m_CornerHandlers)
                {
                    totalSize += columnCornerHandler.Count;
                }
                return totalSize;
            }
        }

        public GameObject ViewPrefab;
        public GameObject ColumnPrefab;
        public GameObject VerticalHandlerPrefab;
        public GameObject HorizontalHandlerPrefab;
        public GameObject CornerHandlerPrefab;
        #endregion

        #region Private Methods
        private void OnRectTransformDimensionsChange()
        {
            m_MinimumViewHeight = Mathf.Min(MINIMUM_VIEW_HEIGHT_DEFAULT, GetComponent<RectTransform>().rect.height / ViewNumber);
            m_MinimumViewWidth = Mathf.Min(MINIMUM_VIEW_WIDTH_DEFAULT, GetComponent<RectTransform>().rect.width / ColumnNumber);
            UpdateHandlersMinMaxPositions();
            SetVerticalHandlersPosition();
            SetHorizontalHandlersPosition();
            UpdateAnchors();
        }
        /// <summary>
        /// Update the size and the position of the columns and the views in order to match the position of the handlers
        /// </summary>
        private void UpdateAnchors()
        {
            for (int i = 0; i < ColumnNumber; i++)
            {
                RectTransform column = m_Columns[i].GetComponent<RectTransform>();
                column.anchorMin = new Vector2((i == 0) ? 0 : m_VerticalHandlers[i - 1].Position, column.anchorMin.y);
                column.anchorMax = new Vector2((i == ColumnNumber - 1) ? 1 : m_VerticalHandlers[i].Position, column.anchorMax.y);
                for (int j = 0; j < ViewNumber; j++)
                {
                    if (column.GetComponent<Column>().Views[j] != null)
                    {
                        RectTransform view = column.GetComponent<Column>().Views[j].GetComponent<RectTransform>();
                        view.anchorMin = new Vector2(view.anchorMin.x, (j == ViewNumber - 1) ? 0 : m_HorizontalHandlers[j].Position);
                        view.anchorMax = new Vector2(view.anchorMax.x, (j == 0) ? 1 : m_HorizontalHandlers[j - 1].Position);
                    }
                }
            }
        }
        /// <summary>
        /// Change the position of the vertical handlers next to the selected handler to match order and width constraints
        /// </summary>
        private void SetVerticalHandlersPosition()
        {
            int selectedHandlerID = m_VerticalHandlers.FindIndex((handler) => { return handler.IsClicked; });
            if (selectedHandlerID == -1) return;

            for (int i = 0; i < m_VerticalHandlers.Count; i++)
            {
                float referencePosition = m_VerticalHandlers[selectedHandlerID].Position + (i - selectedHandlerID) * m_MinimumViewWidth / GetComponent<RectTransform>().rect.width;
                if (i < selectedHandlerID)
                {
                    m_VerticalHandlers[i].Position = Mathf.Min(m_VerticalHandlers[i].Position, referencePosition);
                }
                else if (i > selectedHandlerID)
                {
                    m_VerticalHandlers[i].Position = Mathf.Max(m_VerticalHandlers[i].Position, referencePosition);
                }
            }
        }
        /// <summary>
        /// Change the position of the vertical handlers next to the selected handler to match order and height constraints
        /// </summary>
        private void SetHorizontalHandlersPosition()
        {
            int selectedHandlerID = m_HorizontalHandlers.FindIndex((handler) => { return handler.IsClicked; });
            if (selectedHandlerID == -1) return;

            for (int i = 0; i < m_HorizontalHandlers.Count; i++)
            {
                float referencePosition = m_HorizontalHandlers[selectedHandlerID].Position + (selectedHandlerID - i) * m_MinimumViewHeight / GetComponent<RectTransform>().rect.height;
                if (i < selectedHandlerID)
                {
                    m_HorizontalHandlers[i].Position = Mathf.Max(m_HorizontalHandlers[i].Position, referencePosition);
                }
                else if (i > selectedHandlerID)
                {
                    m_HorizontalHandlers[i].Position = Mathf.Min(m_HorizontalHandlers[i].Position, referencePosition);
                }
            }
        }
        /// <summary>
        /// Update the position constraints on the handlers depending on the number of columns and views
        /// </summary>
        private void UpdateHandlersMinMaxPositions()
        {
            for (int i = 0; i < VerticalHandlerNumber; i++)
            {
                m_VerticalHandlers[i].MinimumPosition = (i + 1) * (m_MinimumViewWidth / GetComponent<RectTransform>().rect.width);
                m_VerticalHandlers[i].MaximumPosition = 1 - (VerticalHandlerNumber - i) * (m_MinimumViewWidth / GetComponent<RectTransform>().rect.width);
            }
            for (int i = 0; i < HorizontalHandlerNumber; i++)
            {
                m_HorizontalHandlers[i].MinimumPosition = (HorizontalHandlerNumber - i) * (m_MinimumViewHeight / GetComponent<RectTransform>().rect.height);
                m_HorizontalHandlers[i].MaximumPosition = 1 - (i + 1) * (m_MinimumViewHeight / GetComponent<RectTransform>().rect.height);
            }
            // Update position with new boundaries (call to setter)
            m_VerticalHandlers.ForEach((h) => h.Position = h.Position);
            m_HorizontalHandlers.ForEach((h) => h.Position = h.Position);
        }
        /// <summary>
        /// Set the position of every handler to their respective initial values
        /// </summary>
        private void ResetHandlersPosition()
        {
            for (int i = 0; i < m_VerticalHandlers.Count; i++)
            {
                m_VerticalHandlers[i].Position = (i + 1) / (float)ColumnNumber;
            }
            for (int i = 0; i < m_HorizontalHandlers.Count; i++)
            {
                m_HorizontalHandlers[i].Position = (m_HorizontalHandlers.Count - i) / (float)ViewNumber;
            }
        }
        /// <summary>
        /// Change the index of the elements of the layout in the siblings tree in order to make handlers to always be above views
        /// </summary>
        private void SetIndexOfTransforms()
        {
            int transformIndex = 0;
            foreach (Column column in m_Columns)
            {
                column.transform.SetSiblingIndex(transformIndex++);
            }
            foreach (VerticalHandler verticalHandler in m_VerticalHandlers)
            {
                verticalHandler.transform.SetSiblingIndex(transformIndex++);
            }
            foreach (HorizontalHandler horizontalHandler in m_HorizontalHandlers)
            {
                horizontalHandler.transform.SetSiblingIndex(transformIndex++);
            }
            foreach (List<CornerHandler> columnCornerHandler in m_CornerHandlers)
            {
                foreach (CornerHandler cornerHandler in columnCornerHandler)
                {
                    cornerHandler.transform.SetSiblingIndex(transformIndex++);
                }
            }
        }
        /// <summary>
        /// Update the name of the GameObjects
        /// </summary>
        private void UpdateNameOfGameObjects()
        {
            for (int i = 0; i < ColumnNumber; i++)
            {
                m_Columns[i].name = "Column (" + i + ")";
                for (int j = 0; j < ViewNumber; j++)
                {
                    if (m_Columns[i].Views[j] != null)
                    {
                        m_Columns[i].Views[j].name = "View (" + i + "-" + j + ")";
                    }
                }
            }
            for (int i = 0; i < VerticalHandlerNumber; i++)
            {
                m_VerticalHandlers[i].name = "Vertical Handler (" + i + "-" + (i + 1).ToString() + ")";
            }
            for (int i = 0; i < HorizontalHandlerNumber; i++)
            {
                m_HorizontalHandlers[i].name = "Horizontal Handler (" + i + "-" + (i + 1).ToString() + ")";
            }
            for (int i = 0; i < m_CornerHandlers.Count; i++)
            {
                for (int j = 0; j < m_CornerHandlers[i].Count; j++)
                {
                    m_CornerHandlers[i][j].name = "Corner Handler (" + i + "-" + j + ")";
                }
            }
        }
        /// <summary>
        /// Package of methods to be called when a column or a view is added or removed
        /// </summary>
        private void ChangeNumberOfElementsCallback()
        {
            ResetHandlersPosition();
            SetIndexOfTransforms();
            UpdateHandlersMinMaxPositions();
            UpdateNameOfGameObjects();
            SetVerticalHandlersPosition();
            SetHorizontalHandlersPosition();
            UpdateAnchors();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a column to the layout
        /// </summary>
        public void AddColumn()
        {
            if (ColumnNumber > 0)
            {
                m_VerticalHandlers.Add(Instantiate(VerticalHandlerPrefab, transform).GetComponent<VerticalHandler>());
                m_VerticalHandlers.Last().OnChangePosition.AddListener(() =>
                {
                    SetVerticalHandlersPosition();
                    SetHorizontalHandlersPosition();
                    UpdateAnchors();
                });
                m_CornerHandlers.Add(new List<CornerHandler>());
                for (int i = 0; i < HorizontalHandlerNumber; i++)
                {
                    m_CornerHandlers.Last().Add(Instantiate(CornerHandlerPrefab, transform).GetComponent<CornerHandler>());
                    m_CornerHandlers.Last().Last().Initialize(m_VerticalHandlers.Last(), m_HorizontalHandlers[i]);
                }
            }
            m_Columns.Add(Instantiate(ColumnPrefab, transform).GetComponent<Column>());

            for (int i = 1; i < ViewNumber; i++)
            {
                m_Columns.Last().AddView();
            }

            ChangeNumberOfElementsCallback();
        }
        /// <summary>
        /// Remove a column from the layout
        /// </summary>
        /// <param name="column">Column to be removed</param>
        public void RemoveColumn(Column column)
        {
            if (ColumnNumber > 1 && column != null)
            {
                int columnCornerHandlerIndex = m_Columns.FindIndex((c) => { return c == column; }) - 1;

                Destroy(column.gameObject);
                m_Columns.Remove(column);

                Destroy(m_VerticalHandlers.Last().gameObject);
                m_VerticalHandlers.Remove(m_VerticalHandlers.Last());
                
                foreach (CornerHandler cornerHandler in m_CornerHandlers[columnCornerHandlerIndex])
                {
                    Destroy(cornerHandler.gameObject);
                }
                m_CornerHandlers.RemoveAt(columnCornerHandlerIndex);

                ChangeNumberOfElementsCallback();
            }
        }
        /// <summary>
        /// Add a view to every columns
        /// </summary>
        public void AddViewLine()
        {
            if (ViewNumber > 0)
            {
                m_HorizontalHandlers.Add(Instantiate(HorizontalHandlerPrefab, transform).GetComponent<HorizontalHandler>());
                m_HorizontalHandlers.Last().OnChangePosition.AddListener(() =>
                {
                    SetVerticalHandlersPosition();
                    SetHorizontalHandlersPosition();
                    UpdateAnchors();
                });
                for (int i = 0; i < VerticalHandlerNumber; i++)
                {
                    m_CornerHandlers[i].Add(Instantiate(CornerHandlerPrefab, transform).GetComponent<CornerHandler>());
                    m_CornerHandlers[i].Last().Initialize(m_VerticalHandlers[i], m_HorizontalHandlers.Last());
                }
            }

            foreach (Column column in m_Columns)
            {
                column.AddView();
            }

            ChangeNumberOfElementsCallback();
        }
        /// <summary>
        /// Remove a view from every columns
        /// </summary>
        /// <param name="lineID">ID of the view line to be removed</param>
        public void RemoveViewLine(int lineID)
        {
            if (ViewNumber > 1)
            {
                foreach (Column column in m_Columns)
                {
                    column.RemoveView(lineID);
                }

                Destroy(m_HorizontalHandlers.Last().gameObject);
                m_HorizontalHandlers.Remove(m_HorizontalHandlers.Last());

                int lineCornerHandlerIndex = lineID - 1;
                foreach (List<CornerHandler> columnCornerHandler in m_CornerHandlers)
                {
                    Destroy(columnCornerHandler[lineCornerHandlerIndex].gameObject);
                    columnCornerHandler.RemoveAt(lineCornerHandlerIndex);
                }

                ChangeNumberOfElementsCallback();
            }
        }
        #endregion
    }
}