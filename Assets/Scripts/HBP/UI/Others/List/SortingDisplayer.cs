using UnityEngine;
using UnityEngine.UI;

public class SortingDisplayer : MonoBehaviour
{
    #region Properties
    [SerializeField] Image m_AscendingImage;
    [SerializeField] Image m_DescendingImage;
    Color m_AscendingColor;
    Color m_DescendingColor;

    public enum SortingType { None, Ascending, Descending }
    SortingType m_Sorting = SortingType.None;
    public SortingType Sorting
    {
        get { return m_Sorting; }
        set
        {
            m_Sorting = value;
            switch (value)
            {
                case SortingType.None:
                    m_AscendingImage.color = m_AscendingColor;
                    m_DescendingImage.color = m_DescendingColor;
                    break;
                case SortingType.Ascending:
                    m_AscendingColor = m_AscendingImage.color;
                    m_AscendingImage.color = ApplicationState.Theme.Window.Content.Toggle.Checkmark;
                    m_DescendingImage.color = m_DescendingColor;
                    break;
                case SortingType.Descending:
                    m_DescendingColor = m_DescendingImage.color;
                    m_AscendingImage.color = m_AscendingColor;
                    m_DescendingImage.color = ApplicationState.Theme.Window.Content.Toggle.Checkmark;
                    break;
            }
        }
    }
    #endregion

    #region Private Methods
    void Awake()
    {
        m_AscendingColor = m_AscendingImage.color;
        m_DescendingColor = m_DescendingImage.color;
    }
    #endregion
}
