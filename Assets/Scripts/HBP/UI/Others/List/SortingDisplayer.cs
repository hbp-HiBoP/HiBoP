using NewTheme.Components;
using UnityEngine;

public class SortingDisplayer : MonoBehaviour
{
    #region Properties
    [SerializeField] State Activated;
    [SerializeField] ThemeElement m_AscendingThemeElement;
    [SerializeField] ThemeElement m_DescendingThemeElement;

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
                    m_AscendingThemeElement.Set();
                    m_DescendingThemeElement.Set();
                    break;
                case SortingType.Ascending:
                    m_AscendingThemeElement.Set(Activated);
                    m_DescendingThemeElement.Set();
                    break;
                case SortingType.Descending:
                    m_AscendingThemeElement.Set();
                    m_DescendingThemeElement.Set(Activated);
                    break;
            }
        }
    }
    #endregion
}
