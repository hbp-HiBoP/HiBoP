using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ColumnResizer : OverlayElement, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        [SerializeField]
        private Button m_Expand;
        [SerializeField]
        private Button m_Minimize;
        #endregion

        #region Public Methods
        public override void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Initialize(scene, column, columnUI);
            m_Expand.onClick.AddListener(() =>
            {
                columnUI.Expand();
            });
            m_Minimize.onClick.AddListener(() =>
            {
                columnUI.Minimize();
            });
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_Expand.gameObject.SetActive(true);
            m_Minimize.gameObject.SetActive(true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            m_Expand.gameObject.SetActive(false);
            m_Minimize.gameObject.SetActive(false);
        }
        #endregion
    }
}