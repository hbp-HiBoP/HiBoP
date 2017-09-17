using UnityEngine;
using UnityEngine.UI;

public class SortingDisplayer : MonoBehaviour
{
    #region Properties
    [SerializeField] Image m_AscendingImage;
    [SerializeField] Image m_DescendingImage;

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
                    m_AscendingImage.color = ApplicationState.Theme.Window.Content.Text.Color;
                    m_DescendingImage.color = ApplicationState.Theme.Window.Content.Text.Color;
                    break;
                case SortingType.Ascending:
                    m_AscendingImage.color = ApplicationState.Theme.Window.Content.Toggle.Checkmark;
                    m_DescendingImage.color = ApplicationState.Theme.Window.Content.Text.Color;
                    break;
                case SortingType.Descending:
                    m_AscendingImage.color = ApplicationState.Theme.Window.Content.Text.Color;
                    m_DescendingImage.color = ApplicationState.Theme.Window.Content.Toggle.Checkmark;
                    break;
            }
        }
    }
    #endregion
}
