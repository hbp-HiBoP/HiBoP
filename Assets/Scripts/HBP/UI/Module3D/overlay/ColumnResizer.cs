using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ColumnResizer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        [SerializeField]
        private Button m_Expand;
        [SerializeField]
        private Button m_Minimize;
        #endregion

        #region Public Methods
        public void Initialize(Column3DUI columnUI)
        {
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