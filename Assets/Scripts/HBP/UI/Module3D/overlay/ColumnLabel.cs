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
    public class ColumnLabel : OverlayElement, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        [SerializeField]
        private Button m_Left;
        [SerializeField]
        private Button m_Right;

        private bool m_RectTransformChanged;

        [SerializeField]
        private GameObject m_ColumnImagePrefab;
        private GameObject m_CurrentImage;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_RectTransformChanged)
            {
                if (m_RectTransform.rect.width < 40)
                {
                    m_Left.gameObject.SetActive(false);
                    m_Text.gameObject.SetActive(false);
                    m_Right.gameObject.SetActive(false);
                }
                else if (!m_Left.gameObject.activeSelf && !m_Right.gameObject.activeSelf && !m_Text.gameObject.activeSelf)
                {
                    m_Left.gameObject.SetActive(true);
                    m_Text.gameObject.SetActive(true);
                    m_Right.gameObject.SetActive(true);
                }
                m_RectTransformChanged = false;
            }
            if (m_CurrentImage)
            {
                m_CurrentImage.transform.position = Input.mousePosition;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Initialize(scene, column, columnUI);
            IsActive = false;

            m_Text.text = column.Label;
            m_Left.onClick.AddListener(() =>
            {
                columnUI.Move(-1);
            });
            m_Right.onClick.AddListener(() =>
            {
                columnUI.Move(+1);
            });

            switch (column.Type)
            {
                case Column3D.ColumnType.Base:
                    IsActive = false;
                    break;
                case Column3D.ColumnType.IEEG:
                    IsActive = true;
                    break;
                default:
                    break;
            }
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_CurrentImage)
            {
                Destroy(m_CurrentImage);
            }
            m_CurrentImage = Instantiate(m_ColumnImagePrefab, m_ColumnUI.ParentGrid.transform);
            m_CurrentImage.transform.Find("Label").GetComponent<Text>().text = m_ColumnUI.Column.Label;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_CurrentImage)
            {
                Destroy(m_CurrentImage);
                m_ColumnUI.SwapColumnWithHoveredColumn();
                m_ColumnUI.UpdateBorderVisibility(true);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            m_ColumnUI.UpdateBorderVisibility();
        }

        public void OnRectTransformDimensionsChange()
        {
            m_RectTransformChanged = true;
        }
        #endregion
    }
}