using UnityEngine;
using UnityEngine.EventSystems;

public class PointerDebug : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    #region Properties
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
    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_CurrentImage)
        {
            Destroy(m_CurrentImage);
        }
        m_CurrentImage = Instantiate(m_ColumnImagePrefab, transform.parent);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (m_CurrentImage)
        {
            Destroy(m_CurrentImage);
        }
    }
    #endregion
}