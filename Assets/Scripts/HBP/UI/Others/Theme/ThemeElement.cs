using UnityEngine;

public class ThemeElement : MonoBehaviour
{
    public enum ElementType { None, WindowBackground , WindowHeaderBackground, WindowHeaderTitle, WindowTitleBackground,
        WindowTitle, WindowLabel, WindowInputField, WindowFolderSelector, WindowGeneralButton, WindowOtherButton,
        WindowToggle, WindowListBackground, WindowScrollbar , WindowDropdown }

    [SerializeField]
    ElementType type;
    public ElementType Type { get { return type; } set { type = value; } }
}
