using HBP.Display.Module3D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Overlay element to display the name of the column and allow swapping the columns on the UI
    /// </summary>
    public class ColumnLabel : ColumnOverlayElement, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        #region Properties
        /// <summary>
        /// Displays the name of the column
        /// </summary>
        [SerializeField] private Text m_Text;
        /// <summary>
        /// Button to swap this column with the one at the left
        /// </summary>
        [SerializeField] private Button m_Left;
        /// <summary>
        /// Button to swap this column with the one at the right
        /// </summary>
        [SerializeField] private Button m_Right;

        /// <summary>
        /// Prefab of the object used when drag and dropping to swap the column
        /// </summary>
        [SerializeField] private GameObject m_ColumnImagePrefab;
        /// <summary>
        /// Currently instanced object used when drag and dropping to swap the column
        /// </summary>
        private GameObject m_CurrentImage;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_RectTransform.hasChanged)
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
                m_RectTransform.hasChanged = false;
            }
            if (m_CurrentImage)
            {
                m_CurrentImage.transform.position = Input.mousePosition;
            }
        }
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
            IsActive = true;

            m_Text.text = column.Name;
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
            m_CurrentImage = Instantiate(m_ColumnImagePrefab, m_ColumnUI.ParentGrid.transform);
            m_CurrentImage.transform.Find("Label").GetComponent<Text>().text = m_ColumnUI.Column.Name;
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
        #endregion
    }
}