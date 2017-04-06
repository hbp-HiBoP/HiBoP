using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Theme
{
    public class Theme
    {
        public ThemeColor Color { get; set; }
        public ThemeFont Font { get; set; }

        public Theme(ThemeColor color,ThemeFont fontSkin)
        {
            Color = color;
            Font = fontSkin;
        }
        public Theme() : this(new ThemeColor(),new ThemeFont())
        {
        }
    }
    public class ThemeColor
    {
        // Color Background.
        public Color WindowBackground { get; set; }
        public Color WindowHeaderBackground { get; set; }
        public Color WindowContentTitleBackground { get; set; }

        // Color inputfield.
        public ColorBlock InputField { get; set; }

        // Color Lists.
        public Color ListBackground { get; set; }
        public ColorBlock Scrollbar { get; set; }
        public Color ScrollbarBackground { get; set; }

        // Color buttons.
        public ColorBlock GeneralButton { get; set; }
        public ColorBlock OtherButton { get; set; }
        public ColorBlock FolderSelectorButton { get; set; }
        public ColorBlock MenuButton { get; set; }

        // Color Toggle.
        public ColorBlock WindowToggle { get; set; }
        public ColorBlock MenuToggle { get; set; }

        // Color Dropdown.
        public ColorBlock WindowDropdown { get; set; }
        public ColorBlock MenuDropdown { get; set; }

        // Color text.
        public Color ContentNormalLabel { get; set; }
        public Color ContentTitleLabel { get; set; }
        public Color ContentGeneralButtonLabel { get; set; }
        public Color ContentOtherButtonLabel { get; set; }
        public Color HeaderTitleLabel { get; set; }

        public ThemeColor()
        {
            WindowBackground = new Color(56, 56, 56, 255) / 255.0f;
            WindowHeaderBackground = new Color(41, 41, 41, 255) / 255.0f;
            WindowContentTitleBackground = new Color(80, 80, 80, 255) / 255.0f;

            ColorBlock inputField = ColorBlock.defaultColorBlock;
            inputField.normalColor = new Color(65, 65, 65, 255) / 255.0f;
            inputField.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            inputField.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            inputField.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            InputField = inputField;

            ListBackground = new Color(80, 80, 80, 255) / 255.0f;
            ColorBlock scrollbar = ColorBlock.defaultColorBlock;
            scrollbar.normalColor = new Color(65, 65, 65, 255) / 255.0f;
            scrollbar.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            scrollbar.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            scrollbar.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            Scrollbar = scrollbar;
            ScrollbarBackground = new Color(40, 40, 40, 255) / 255.0f;

            ColorBlock generalButton = ColorBlock.defaultColorBlock;
            generalButton.normalColor = new Color(255, 255, 255, 255) / 255.0f;
            generalButton.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
            generalButton.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
            generalButton.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
            GeneralButton = generalButton;

            ColorBlock otherButton = ColorBlock.defaultColorBlock;
            otherButton.normalColor = new Color(65, 65, 65, 255) / 255.0f;
            otherButton.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            otherButton.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            otherButton.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            OtherButton = otherButton;

            ColorBlock menuButton = ColorBlock.defaultColorBlock;
            menuButton.normalColor = new Color(65, 65, 65, 0) / 255.0f;
            menuButton.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            menuButton.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            menuButton.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            MenuButton = menuButton;

            ColorBlock windowToggle = ColorBlock.defaultColorBlock;
            windowToggle.normalColor = new Color(65, 65, 65, 255) / 255.0f;
            windowToggle.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
            windowToggle.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
            windowToggle.disabledColor = new Color(65, 65, 65, 255) / 255.0f;
            WindowToggle = windowToggle;

            ColorBlock menuToggle = ColorBlock.defaultColorBlock;
            menuToggle.normalColor = new Color(65, 65, 65, 0) / 255.0f;
            menuToggle.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            menuToggle.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            menuToggle.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            MenuToggle = menuToggle;

            ColorBlock windowDropdown = ColorBlock.defaultColorBlock;
            windowDropdown.normalColor = new Color(65, 65, 65, 255) / 255.0f;
            windowDropdown.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            windowDropdown.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            windowDropdown.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            WindowDropdown = windowDropdown;

            ColorBlock menuDropdown = ColorBlock.defaultColorBlock;
            menuDropdown.normalColor = new Color(65, 65, 65, 0) / 255.0f;
            menuDropdown.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
            menuDropdown.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
            menuDropdown.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            MenuDropdown = menuDropdown;

            ColorBlock folderSelectorButton = ColorBlock.defaultColorBlock;
            folderSelectorButton.normalColor = new Color(255, 255, 255, 255) / 255.0f;
            folderSelectorButton.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
            folderSelectorButton.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
            folderSelectorButton.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
            FolderSelectorButton = folderSelectorButton;

            HeaderTitleLabel = new Color(255, 255, 255, 255) / 255.0f;
            ContentNormalLabel = new Color(255, 255, 255, 255) / 255.0f;
            ContentTitleLabel = new Color(255, 255, 255, 255) / 255.0f;
            ContentGeneralButtonLabel = new Color(0, 0, 0, 255) / 255.0f;

            ContentOtherButtonLabel = new Color(255, 255, 255, 255) / 255.0f;
        }
    }
    public class ThemeFont
    {
        public FontData Menu { get; set; }
        public FontData WindowHeader { get; set; }
        public FontData WindowContentTitle { get; set; }
        public FontData WindowContentLabel { get; set; }
        public FontData WindowContentGeneralButton { get; set; }
        public FontData WindowContentOtherButton { get; set; }

        public ThemeFont()
        {
            WindowHeader = FontData.defaultFontData;
            WindowHeader.fontSize = 14;
            WindowHeader.font = Resources.Load<Font>("Fonts/Arial");
            WindowHeader.alignByGeometry = true;
            WindowHeader.alignment = TextAnchor.MiddleCenter;
            WindowHeader.fontStyle = FontStyle.Bold;

            WindowContentTitle = FontData.defaultFontData;
            WindowContentTitle.fontSize = 14;
            WindowContentTitle.font = Resources.Load<Font>("Fonts/Arial");
            WindowContentTitle.alignByGeometry = true;
            WindowContentTitle.alignment = TextAnchor.MiddleLeft;
            WindowContentTitle.fontStyle = FontStyle.Normal;

            WindowContentLabel = FontData.defaultFontData;
            WindowContentLabel.fontSize = 14;
            WindowContentLabel.font = Resources.Load<Font>("Fonts/Arial");
            WindowContentLabel.alignByGeometry = true;
            WindowContentLabel.alignment = TextAnchor.MiddleLeft;
            WindowContentLabel.fontStyle = FontStyle.Normal;

            Menu = FontData.defaultFontData;
            Menu.fontSize = 14;
            Menu.font = Resources.Load<Font>("Fonts/Arial");
            Menu.alignByGeometry = true;
            Menu.alignment = TextAnchor.MiddleLeft;
            Menu.fontStyle = FontStyle.Normal;

            WindowContentGeneralButton = FontData.defaultFontData;
            WindowContentGeneralButton.fontSize = 14;
            WindowContentGeneralButton.font = Resources.Load<Font>("Fonts/Arial");
            WindowContentGeneralButton.alignByGeometry = true;
            WindowContentGeneralButton.alignment = TextAnchor.MiddleCenter;
            WindowContentGeneralButton.fontStyle = FontStyle.Bold;

            WindowContentOtherButton = FontData.defaultFontData;
            WindowContentOtherButton.fontSize = 14;
            WindowContentOtherButton.font = Resources.Load<Font>("Fonts/Arial");
            WindowContentOtherButton.alignByGeometry = true;
            WindowContentOtherButton.alignment = TextAnchor.MiddleCenter;
            WindowContentOtherButton.fontStyle = FontStyle.Normal;
        }
    }
}