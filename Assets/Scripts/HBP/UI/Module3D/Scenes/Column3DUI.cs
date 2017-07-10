using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
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
        /// <summary>
        /// Parent resizable grid
        /// </summary>
        private ResizableGrid m_ParentGrid;
        /// <summary>
        /// Reference to this object's RectTransform
        /// </summary>
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
        /// Is the column initialized ?
        /// </summary>
        private bool m_IsInitialized = false;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ParentGrid = GetComponentInParent<ResizableGrid>();
            m_RectTransform = GetComponent<RectTransform>();
        }
        private void Update()
        {
            m_MinimizedGameObject.transform.SetAsLastSibling();
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

            // FIXME : maybe performance gain possible, or maybe this should be an option
            if (m_Colormap.IsActive && m_TimeDisplay.IsActive && m_Icon.IsActive)
            {
                if (m_RectTransform.rect.width < MINIMUM_SIZE_TO_DISPLAY_OVERLAY)
                {
                    m_Colormap.gameObject.SetActive(false);
                    m_TimeDisplay.gameObject.SetActive(false);
                    m_Icon.gameObject.SetActive(false);
                }
                else
                {
                    m_Colormap.gameObject.SetActive(true);
                    m_TimeDisplay.gameObject.SetActive(true);
                    m_Icon.gameObject.SetActive(true);
                }
            }

            if (Mathf.Abs(m_RectTransform.rect.width - m_ParentGrid.MinimumViewWidth) <= 0.9f)
            {
                m_MinimizedGameObject.SetActive(true);
            }
            else
            {
                m_MinimizedGameObject.SetActive(false);
            }
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
            m_Colormap.Initialize(scene, column);
            m_Colormap.gameObject.SetActive(false);
            m_TimeDisplay.Initialize(scene, column);
            m_TimeDisplay.gameObject.SetActive(false);
            m_Icon.Initialize(scene, column);
            m_Icon.gameObject.SetActive(false);
            m_Label.Initialize(scene, column);
            m_IsInitialized = true;
        }
        #endregion
    }
}