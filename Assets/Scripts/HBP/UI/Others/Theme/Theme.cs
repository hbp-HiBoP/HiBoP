using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Theme
{
    [CreateAssetMenu(menuName = "Theme/New")]
    public class Theme : ScriptableObject
    {
        public ThemeMenu Menu = new ThemeMenu();
        public ThemeColor Color = new ThemeColor();
        public ThemeFont Font = new ThemeFont();

        private void OnEnable()
        {
            Menu.Initialize();
            Color.Initialize();
            Font.Initialize();
        }

        [System.Serializable]
        public class ThemeColor
        {
            public BackgroundTheme Background;

            // Color inputfield.
            public ThemeColorBlock InputField = new ThemeColorBlock();
            public ThemeColorBlock ToolbarInputField = new ThemeColorBlock();

            // Color Lists.
            public ThemeColorBlock Scrollbar = new ThemeColorBlock();

            // Color buttons.
            public ThemeColorBlock GeneralButton = new ThemeColorBlock();
            public ThemeColorBlock OtherButton = new ThemeColorBlock();
            public ThemeColorBlock FolderSelectorButton = new ThemeColorBlock();
            public ThemeColorBlock ToolbarButton = new ThemeColorBlock();

            // Color Toggle.
            public ThemeColorBlock WindowToggle = new ThemeColorBlock();
            public ThemeColorBlock MenuToggle = new ThemeColorBlock();
            public ThemeColorBlock ToolbarToggle = new ThemeColorBlock();
            public Color CheckMarkColor = new Color();

            // Color Dropdown.
            public ThemeColorBlock WindowDropdown = new ThemeColorBlock();
            public ThemeColorBlock MenuDropdown = new ThemeColorBlock();
            public ThemeColorBlock ToolbarDropdownText = new ThemeColorBlock();
            public ThemeColorBlock ToolbarDropdownImage = new ThemeColorBlock();

            // Color text.
            public Color ContentNormalLabel = new Color();
            public Color ContentTitleLabel = new Color();
            public Color ContentGeneralButtonLabel = new Color();
            public Color ContentOtherButtonLabel = new Color();
            public Color HeaderTitleLabel = new Color();
            public Color DisableLabel = new Color();
            public Color ToolbarLabel = new Color();

            // Color Toolbar Slider
            public Color SliderBackground = new Color();
            public Color SliderFill = new Color();
            public Color SliderHandle = new Color();

            // Color 3D Views
            public Color RegularViewColor = new Color();
            public Color SelectedViewColor = new Color();
            public Color ClickedViewColor = new Color();

            // Ok/NotOk
            public Color Error = new Color();
            public Color OK = new Color();
            public Color NotInteractable = new Color();

            public void Initialize()
            {
                Background.Initialize();

                ColorBlock inputField = ColorBlock.defaultColorBlock;
                inputField.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                inputField.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                inputField.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                inputField.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                InputField.ColorBlock = inputField;

                ColorBlock toolbarInputField = ColorBlock.defaultColorBlock;
                toolbarInputField.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                toolbarInputField.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                toolbarInputField.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                toolbarInputField.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                ToolbarInputField.ColorBlock = toolbarInputField;

                ColorBlock scrollbar = ColorBlock.defaultColorBlock;
                scrollbar.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                scrollbar.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                scrollbar.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                scrollbar.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Scrollbar.ColorBlock = scrollbar;

                ColorBlock generalButton = ColorBlock.defaultColorBlock;
                generalButton.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                generalButton.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                generalButton.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                generalButton.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                GeneralButton.ColorBlock = generalButton;

                ColorBlock otherButton = ColorBlock.defaultColorBlock;
                otherButton.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                otherButton.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                otherButton.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                otherButton.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                OtherButton.ColorBlock = otherButton;



                ColorBlock toolbarButton = ColorBlock.defaultColorBlock;
                toolbarButton.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                toolbarButton.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                toolbarButton.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                toolbarButton.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                ToolbarButton.ColorBlock = toolbarButton;

                ColorBlock windowToggle = ColorBlock.defaultColorBlock;
                windowToggle.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                windowToggle.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                windowToggle.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                windowToggle.disabledColor = new Color(65, 65, 65, 255) / 255.0f;
                WindowToggle.ColorBlock = windowToggle;

                ColorBlock menuToggle = ColorBlock.defaultColorBlock;
                menuToggle.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                menuToggle.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                menuToggle.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                menuToggle.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                MenuToggle.ColorBlock = menuToggle;
                CheckMarkColor = new Color(59, 122, 194, 255) / 255.0f;

                ColorBlock toolbarToggle = ColorBlock.defaultColorBlock;
                toolbarToggle.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                toolbarToggle.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                toolbarToggle.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                toolbarToggle.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                ToolbarToggle.ColorBlock = toolbarToggle;

                ColorBlock windowDropdown = ColorBlock.defaultColorBlock;
                windowDropdown.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                windowDropdown.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                windowDropdown.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                windowDropdown.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                WindowDropdown.ColorBlock = windowDropdown;

                ColorBlock menuDropdown = ColorBlock.defaultColorBlock;
                menuDropdown.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                menuDropdown.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                menuDropdown.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                menuDropdown.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                MenuDropdown.ColorBlock = menuDropdown;

                ColorBlock toolbarDropdown = ColorBlock.defaultColorBlock;
                toolbarDropdown.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                toolbarDropdown.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                toolbarDropdown.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                toolbarDropdown.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                ToolbarDropdownText.ColorBlock = toolbarDropdown;

                ColorBlock toolbarDropdownImage = ColorBlock.defaultColorBlock;
                toolbarDropdownImage.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                toolbarDropdownImage.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                toolbarDropdownImage.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                toolbarDropdownImage.disabledColor = new Color(255, 255, 255, 255) / 255.0f;
                ToolbarDropdownImage.ColorBlock = toolbarDropdownImage;

                ColorBlock folderSelectorButton = ColorBlock.defaultColorBlock;
                folderSelectorButton.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                folderSelectorButton.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                folderSelectorButton.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                folderSelectorButton.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                FolderSelectorButton.ColorBlock = folderSelectorButton;

                HeaderTitleLabel = new Color(255, 255, 255, 255) / 255.0f;
                ContentNormalLabel = new Color(255, 255, 255, 255) / 255.0f;
                ContentTitleLabel = new Color(255, 255, 255, 255) / 255.0f;
                ContentGeneralButtonLabel = new Color(0, 0, 0, 255) / 255.0f;
                ContentOtherButtonLabel = new Color(255, 255, 255, 255) / 255.0f;
                DisableLabel = new Color(200, 50, 50, 255) / 255.0f;
                ToolbarLabel = new Color(255, 255, 255, 255) / 255.0f;

                SliderBackground = new Color(255, 255, 255, 255) / 255.0f;
                SliderFill = new Color(255, 255, 255, 255) / 255.0f;
                SliderHandle = new Color(170, 170, 170, 255) / 255.0f;


                RegularViewColor = new Color(212, 212, 212, 255) / 255.0f;
                SelectedViewColor = new Color(156, 187, 227, 255) / 255.0f;
                ClickedViewColor = new Color(59, 122, 194, 255) / 255.0f;

                OK = new Color(50, 200, 50, 255) / 255.0f;
                Error = new Color(200, 50, 50, 255) / 255.0f;
            }
        }

        [System.Serializable]
        public struct ThemeFont
        {
            public ThemeFontData Menu;
            public ThemeFontData Toolbar;
            public ThemeFontData WindowHeader;
            public ThemeFontData WindowContentTitle;
            public ThemeFontData WindowContentLabel;
            public ThemeFontData WindowContentGeneralButton;
            public ThemeFontData WindowContentOtherButton;

            public void Initialize()
            {
                FontData windowHeader = FontData.defaultFontData;
                windowHeader.fontSize = 14;
                windowHeader.font = Resources.Load<Font>("Fonts/Arial");
                windowHeader.alignByGeometry = true;
                windowHeader.alignment = TextAnchor.MiddleCenter;
                windowHeader.fontStyle = FontStyle.Bold;
                WindowHeader.FontData = windowHeader;

                FontData windowContentTitle = FontData.defaultFontData;
                windowContentTitle.fontSize = 14;
                windowContentTitle.font = Resources.Load<Font>("Fonts/Arial");
                windowContentTitle.alignByGeometry = true;
                windowContentTitle.alignment = TextAnchor.MiddleLeft;
                windowContentTitle.fontStyle = FontStyle.Normal;
                WindowContentTitle.FontData = windowContentTitle;

                FontData windowContentLabel = FontData.defaultFontData;
                windowContentLabel.fontSize = 14;
                windowContentLabel.font = Resources.Load<Font>("Fonts/Arial");
                windowContentLabel.alignByGeometry = true;
                windowContentLabel.alignment = TextAnchor.MiddleLeft;
                windowContentLabel.fontStyle = FontStyle.Normal;
                WindowContentLabel.FontData = windowContentLabel;



                FontData windowContentGeneralButton = FontData.defaultFontData;
                windowContentGeneralButton.fontSize = 14;
                windowContentGeneralButton.font = Resources.Load<Font>("Fonts/Arial");
                windowContentGeneralButton.alignByGeometry = true;
                windowContentGeneralButton.alignment = TextAnchor.MiddleCenter;
                windowContentGeneralButton.fontStyle = FontStyle.Bold;
                WindowContentGeneralButton.FontData = windowContentGeneralButton;

                FontData windowContentOtherButton = FontData.defaultFontData;
                windowContentOtherButton.fontSize = 14;
                windowContentOtherButton.font = Resources.Load<Font>("Fonts/Arial");
                windowContentOtherButton.alignByGeometry = true;
                windowContentOtherButton.alignment = TextAnchor.MiddleCenter;
                windowContentOtherButton.fontStyle = FontStyle.Normal;
                WindowContentOtherButton.FontData = windowContentOtherButton;

                FontData toolbar = FontData.defaultFontData;
                toolbar.fontSize = 14;
                toolbar.font = Resources.Load<Font>("Fonts/Arial");
                toolbar.alignByGeometry = true;
                toolbar.alignment = TextAnchor.MiddleCenter;
                toolbar.fontStyle = FontStyle.Normal;
                Toolbar.FontData = toolbar;
            }
        }

        [System.Serializable]
        public struct ThemeLabel
        {
            public FontData Font;
            public Color Color;
        }

        [System.Serializable]
        public struct ThemeColorBlock
        {
            public ColorBlock ColorBlock;
        }

        [System.Serializable]
        public struct ThemeFontData
        {
            public FontData FontData;
        }

        [System.Serializable]
        public struct BackgroundTheme
        {
            // Color Background.
            public Color Window;
            public Color Header;
            public Color Title;
            public Color ToolsBar;
            public Color List;
            public Color ScrollBar;


            public void Initialize()
            {
                Window = new Color(56, 56, 56, 255) / 255.0f;
                Header = new Color(41, 41, 41, 255) / 255.0f;
                Title = new Color(80, 80, 80, 255) / 255.0f;
                ToolsBar = new Color(40, 40, 40, 255) / 255.0f;
                List = new Color(80, 80, 80, 255) / 255.0f;
                ScrollBar = new Color(40, 40, 40, 255) / 255.0f;
            }
        }

        [System.Serializable]
        public struct ThemeMenu
        {
            public Color Background;
            public ThemeColorBlock Button;
            public ThemeLabel Label;

            public void Initialize()
            {
                Background = new Color(40, 40, 40, 255) / 255.0f;

                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(30, 30, 30, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(40, 40, 40, 255) / 255.0f;

                Label.Color = new Color(255, 255, 255, 255) / 255.0f;
                Label.Font = FontData.defaultFontData;
                Label.Font.fontSize = 13;
                Label.Font.font = Resources.Load<Font>("Fonts/Arial");
                Label.Font.alignByGeometry = true;
                Label.Font.fontStyle = FontStyle.Normal;
            }
        }
    }
}