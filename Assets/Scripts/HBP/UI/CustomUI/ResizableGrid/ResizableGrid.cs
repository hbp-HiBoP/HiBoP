using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

namespace HBP.UI.ResizableGrid
{
    public class ResizableGrid : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private RectTransform m_RectTransform;
        /// <summary>
        /// ResizableGrid's RectTransform
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = GetComponent<RectTransform>();
                }
                return m_RectTransform;
            }
        }

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

        /// <summary>
        /// Threshold for the magntic attraction of the handlers
        /// </summary>
        private const float MAGNETIC_THRESHOLD = 0.020f;

        private const float MINIMUM_VIEW_HEIGHT_DEFAULT = 25.0f;
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

        private const float MINIMUM_VIEW_WIDTH_DEFAULT = 25.0f;
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

        /// <summary>
        /// True if a handler is selected for a drag (to prevent updating the cursor)
        /// </summary>
        public bool IsHandlerClicked { get; set; }

        public GameObject ViewPrefab;
        public GameObject ColumnPrefab;
        public GameObject VerticalHandlerPrefab;
        public GameObject HorizontalHandlerPrefab;
        public GameObject CornerHandlerPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_RectTransform.anchorMin = Vector2.zero;
            m_RectTransform.anchorMax = Vector2.one;
            m_RectTransform.anchoredPosition = Vector2.zero;
            m_RectTransform.sizeDelta = Vector2.zero;
            m_RectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        private void Update()
        {
            //if (m_RectTransform.hasChanged)
            //{
            //    m_MinimumViewHeight = Mathf.Min(MINIMUM_VIEW_HEIGHT_DEFAULT, m_RectTransform.rect.height / ViewNumber);
            //    m_MinimumViewWidth = Mathf.Min(MINIMUM_VIEW_WIDTH_DEFAULT, m_RectTransform.rect.width / ColumnNumber);
            //    UpdateHandlersMinMaxPositions();
            //    SetVerticalHandlersPosition();
            //    SetHorizontalHandlersPosition();
            //    UpdateAnchors();
            //    m_RectTransform.hasChanged = false;
            //}
        }
        private void OnRectTransformDimensionsChange()
        {
            m_MinimumViewHeight = Mathf.Min(MINIMUM_VIEW_HEIGHT_DEFAULT, m_RectTransform.rect.height / ViewNumber);
            m_MinimumViewWidth = Mathf.Min(MINIMUM_VIEW_WIDTH_DEFAULT, m_RectTransform.rect.width / ColumnNumber);
            UpdateHandlersMinMaxPositions();
            SetVerticalHandlersPosition();
            SetHorizontalHandlersPosition();
            UpdateAnchors();
        }
        /// <summary>
        /// Update the position constraints on the handlers depending on the number of columns and views
        /// </summary>
        private void UpdateHandlersMinMaxPositions()
        {
            for (int i = 0; i < VerticalHandlerNumber; i++)
            {
                m_VerticalHandlers[i].MinimumPosition = (i + 1) * (m_MinimumViewWidth / m_RectTransform.rect.width);
                m_VerticalHandlers[i].MaximumPosition = 1 - (VerticalHandlerNumber - i) * (m_MinimumViewWidth / m_RectTransform.rect.width);
            }
            for (int i = 0; i < HorizontalHandlerNumber; i++)
            {
                m_HorizontalHandlers[i].MinimumPosition = (HorizontalHandlerNumber - i) * (m_MinimumViewHeight / m_RectTransform.rect.height);
                m_HorizontalHandlers[i].MaximumPosition = 1 - (i + 1) * (m_MinimumViewHeight / m_RectTransform.rect.height);
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
                float defaultPosition = (i + 1) / (float)ColumnNumber;
                m_VerticalHandlers[i].Position = defaultPosition;
                m_VerticalHandlers[i].MagneticPosition = defaultPosition;
                m_VerticalHandlers[i].MagneticThreshold = Mathf.Min(MAGNETIC_THRESHOLD, 0.1f * Mathf.Abs(m_VerticalHandlers[i].MaximumPosition - m_VerticalHandlers[i].MinimumPosition));
            }
            for (int i = 0; i < m_HorizontalHandlers.Count; i++)
            {
                float defaultPosition = (m_HorizontalHandlers.Count - i) / (float)ViewNumber;
                m_HorizontalHandlers[i].Position = defaultPosition;
                m_HorizontalHandlers[i].MagneticPosition = defaultPosition;
                m_HorizontalHandlers[i].MagneticThreshold = Mathf.Min(MAGNETIC_THRESHOLD, 0.1f * Mathf.Abs(m_HorizontalHandlers[i].MaximumPosition - m_HorizontalHandlers[i].MinimumPosition));
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
        /// Update the size and the position of the columns and the views in order to match the position of the handlers
        /// </summary>
        public void UpdateAnchors()
        {
            for (int i = 0; i < ColumnNumber; i++)
            {
                RectTransform column = m_Columns[i].GetComponent<RectTransform>();
                column.anchorMin = new Vector2((i == 0) ? 0 : m_VerticalHandlers[i - 1].Position, column.anchorMin.y);
                column.anchorMax = new Vector2((i == ColumnNumber - 1) ? 1 : m_VerticalHandlers[i].Position, column.anchorMax.y);
                for (int j = 0; j < ViewNumber; j++)
                {
                    if (m_Columns[i].Views[j] != null)
                    {
                        RectTransform view = m_Columns[i].Views[j].GetComponent<RectTransform>();
                        view.anchorMin = new Vector2(view.anchorMin.x, (j == ViewNumber - 1) ? 0 : m_HorizontalHandlers[j].Position);
                        view.anchorMax = new Vector2(view.anchorMax.x, (j == 0) ? 1 : m_HorizontalHandlers[j - 1].Position);
                    }
                }
            }
        }
        /// <summary>
        /// Change the position of the vertical handlers next to the selected handler to match order and width constraints
        /// </summary>
        public void SetVerticalHandlersPosition(int selectedHandlerID = -1)
        {
            if (selectedHandlerID == -1)
            {
                selectedHandlerID = m_VerticalHandlers.FindIndex((handler) => { return handler.IsClicked; });
                if (selectedHandlerID == -1) return;
            }

            for (int i = 0; i < m_VerticalHandlers.Count; i++)
            {
                float referencePosition = m_VerticalHandlers[selectedHandlerID].Position + (i - selectedHandlerID) * m_MinimumViewWidth / m_RectTransform.rect.width;
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
        public void SetHorizontalHandlersPosition(int selectedHandlerID = -1)
        {
            if (selectedHandlerID == -1)
            {
                selectedHandlerID = m_HorizontalHandlers.FindIndex((handler) => { return handler.IsClicked; });
                if (selectedHandlerID == -1) return;
            }

            for (int i = 0; i < m_HorizontalHandlers.Count; i++)
            {
                float referencePosition = m_HorizontalHandlers[selectedHandlerID].Position + (selectedHandlerID - i) * m_MinimumViewHeight / m_RectTransform.rect.height;
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
        /// Reset the positions of the views
        /// </summary>
        public void ResetPositions()
        {
            ResetHandlersPosition();
            SetIndexOfTransforms();
            UpdateHandlersMinMaxPositions();
            UpdateNameOfGameObjects();
            SetVerticalHandlersPosition();
            SetHorizontalHandlersPosition();
            UpdateAnchors();
        }

        /// <summary>
        /// Add a column to the layout
        /// </summary>
        public void AddColumn()
        {
            AddColumn(ColumnPrefab, ViewPrefab);
        }
        public void AddColumn(GameObject customColumnPrefab, GameObject customViewPrefab)
        {
            if (ColumnNumber > 0)
            {
                VerticalHandler verticalHandler = Instantiate(VerticalHandlerPrefab, transform).GetComponent<VerticalHandler>();
                verticalHandler.Initialize(this);
                m_VerticalHandlers.Add(verticalHandler);
                verticalHandler.OnChangePosition.AddListener(() =>
                {
                    SetVerticalHandlersPosition();
                    SetHorizontalHandlersPosition();
                    UpdateAnchors();
                });
                m_CornerHandlers.Add(new List<CornerHandler>());
                for (int i = 0; i < HorizontalHandlerNumber; i++)
                {
                    CornerHandler cornerHandler = Instantiate(CornerHandlerPrefab, transform).GetComponent<CornerHandler>();
                    cornerHandler.Initialize(this);
                    m_CornerHandlers.Last().Add(cornerHandler);
                    cornerHandler.SetCorrespondingHandlers(m_VerticalHandlers.Last(), m_HorizontalHandlers[i]);
                }
            }
            m_Columns.Add(Instantiate(customColumnPrefab?customColumnPrefab:ColumnPrefab, transform).GetComponent<Column>());

            for (int i = 0; i < ViewNumber; i++)
            {
                m_Columns.Last().AddView(customViewPrefab?customViewPrefab:ViewPrefab);
            }

            ChangeNumberOfElementsCallback();
        }
        /// <summary>
        /// Remove a column from the layout
        /// </summary>
        /// <param name="column">Column to be removed</param>
        public void RemoveColumn(Column column)
        {
            if (!column) return;

            if (ColumnNumber > 1)
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
            AddViewLine(ViewPrefab);
        }
        public void AddViewLine(GameObject customViewPrefab)
        {
            if (ViewNumber > 0)
            {
                HorizontalHandler horizontalHandler = Instantiate(HorizontalHandlerPrefab, transform).GetComponent<HorizontalHandler>();
                horizontalHandler.Initialize(this);
                m_HorizontalHandlers.Add(horizontalHandler);
                horizontalHandler.OnChangePosition.AddListener(() =>
                {
                    SetVerticalHandlersPosition();
                    SetHorizontalHandlersPosition();
                    UpdateAnchors();
                });
                for (int i = 0; i < VerticalHandlerNumber; i++)
                {
                    CornerHandler cornerHandler = Instantiate(CornerHandlerPrefab, transform).GetComponent<CornerHandler>();
                    cornerHandler.Initialize(this);
                    m_CornerHandlers[i].Add(cornerHandler);
                    cornerHandler.SetCorrespondingHandlers(m_VerticalHandlers[i], m_HorizontalHandlers.Last());
                }
            }

            foreach (Column column in m_Columns)
            {
                column.AddView(customViewPrefab);
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
        /// <summary>
        /// Swap two columns
        /// </summary>
        /// <param name="column1"></param>
        /// <param name="column2"></param>
        public void SwapColumns(Column column1, Column column2)
        {
            if (m_Columns.Count < 2) return;

            int id1 = m_Columns.FindIndex((col) => col == column1);
            int id2 = m_Columns.FindIndex((col) => col == column2);
            if (id1 == -1 || id2 == -1) return;

            int minID = Mathf.Min(id1, id2);
            int maxID = Mathf.Max(id1, id2);
            
            List<float> widths = new List<float>();
            for (int i = minID; i <= maxID; i++)
            {
                if (i == 0)
                {
                    widths.Add(m_VerticalHandlers[i].Position);
                }
                else if (i == m_VerticalHandlers.Count)
                {
                    widths.Add(1.0f - m_VerticalHandlers[i - 1].Position);
                }
                else
                {
                    widths.Add(m_VerticalHandlers[i].Position - m_VerticalHandlers[i - 1].Position);
                }
            }
            float tmp = widths[0];
            widths[0] = widths[widths.Count - 1];
            widths[widths.Count - 1] = tmp;
            for (int i = minID; i < maxID; i++)
            {
                if (i == 0)
                {
                    m_VerticalHandlers[i].Position = widths[i - minID];
                }
                else
                {
                    m_VerticalHandlers[i].Position = m_VerticalHandlers[i - 1].Position + widths[i - minID];
                }
            }

            m_Columns[id1] = column2;
            m_Columns[id2] = column1;

            SetIndexOfTransforms();
            UpdateNameOfGameObjects();
            UpdateAnchors();
        }
        #endregion
    }
}