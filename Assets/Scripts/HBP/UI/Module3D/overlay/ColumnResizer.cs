using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Two invisible buttons that expand or minimize a column
    /// </summary>
    public class ColumnResizer : ColumnOverlayElement, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// Button used to completely expand a column
        /// </summary>
        [SerializeField] private Button m_Expand;
        /// <summary>
        /// Button used to minimize a column at most
        /// </summary>
        [SerializeField] private Button m_Minimize;
        #endregion

        #region Public Methods
        /// <summary>
        /// Setup the overlay element
        /// </summary>
        /// <param name="scene">Associated 3D scene</param>
        /// <param name="column">Associated 3D column</param>
        /// <param name="columnUI">Parent UI column</param>
        public override void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Setup(scene, column, columnUI);
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