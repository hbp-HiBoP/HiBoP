using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using Tools.Unity.ResizableGrid;

namespace HBP.UI.Module3D
{
    public class ColumnLabel : OverlayElement, IPointerDownHandler, IPointerUpHandler
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        [SerializeField]
        private Button m_Left;
        [SerializeField]
        private Button m_Right;

        [SerializeField]
        private GameObject m_ColumnImagePrefab;
        private GameObject m_CurrentImage;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_CurrentImage)
            {
                m_CurrentImage.transform.position = Input.mousePosition;
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            m_ColumnUI = columnUI;
            IsActive = true;

            m_Text.text = column.Label;
            m_Left.onClick.AddListener(() =>
            {
                columnUI.Move(-1);
            });
            m_Right.onClick.AddListener(() =>
            {
                columnUI.Move(+1);
            });
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_CurrentImage)
            {
                Destroy(m_CurrentImage);
            }
            m_CurrentImage = Instantiate(m_ColumnImagePrefab, m_ColumnUI.ParentGrid.transform);// maybe FIXME
            m_CurrentImage.transform.Find("Label").GetComponent<Text>().text = m_ColumnUI.Column.Label;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_CurrentImage)
            {
                Destroy(m_CurrentImage);
                m_ColumnUI.SwapColumnWithHoveredColumn();
            }
        }
        #endregion
    }
}