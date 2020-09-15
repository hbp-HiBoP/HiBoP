using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class GraphsGrid : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_ItemAndContainerPrefab;
        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private GridLayoutGroup m_GridLayoutGroup;

        private int m_NumberOfColumns = 5;
        #endregion

        #region Public Methods
        public void Display()
        {
            DisplayGrid(20);
        }
        public void SetNumberOfColumns(float numberOfColumns)
        {
            m_NumberOfColumns = (int)numberOfColumns;
            UpdateLayout();
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Display();
        }
        private void Update()
        {
            if (transform.hasChanged)
            {
                UpdateLayout();
                transform.hasChanged = false;
            }
        }
        private void DisplayGrid(int numberOfItems)
        {
            for (int r = 0; r < numberOfItems; r++)
            {
                Instantiate(m_ItemAndContainerPrefab, transform);
            }
        }
        private void UpdateLayout()
        {
            float width = m_RectTransform.rect.width / m_NumberOfColumns;
            float height = width * 0.5f;
            m_GridLayoutGroup.cellSize = new Vector2(width, height);
        }
        #endregion

    }
}