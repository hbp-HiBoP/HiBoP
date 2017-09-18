using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Theme
{
    [CreateAssetMenu(menuName = "Theme/New")]
    public class Theme : ScriptableObject
    {
        public GeneralTheme General = new GeneralTheme();
        public MenuTheme Menu = new MenuTheme();
        public WindowTheme Window = new WindowTheme();
        public ToolbarTheme Toolbar = new ToolbarTheme();
        public VisualizationTheme Visualization = new VisualizationTheme();

        private void OnEnable()
        {
            General.Initialize();
            Menu.Initialize();
            Window.Initialize();
            Toolbar.Initialize();
            Visualization.Initialize();
        }

        [System.Serializable]
        public class GeneralTheme
        {
            public Color Error = new Color();
            public Color OK = new Color();
            public Color NotInteractable = new Color();

            public void Initialize()
            {
                NotInteractable = new Color(100, 100, 100, 255) / 255.0f;
                OK = new Color(50, 200, 50, 255) / 255.0f;
                Error = new Color(200, 50, 50, 255) / 255.0f;
            }
        }

        [System.Serializable]
        public struct MenuTheme
        {
            public Color Background;
            public TextTheme Text;
            public ButtonTheme Button;
            public ToggleTheme Toggle;
            public DropdownTheme Dropdown;

            public void Initialize()
            {
                Background = new Color(40, 40, 40, 255) / 255.0f;

                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(30, 30, 30, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 13;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.fontStyle = FontStyle.Normal;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 13;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.fontStyle = FontStyle.Normal;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;

                Dropdown.ColorBlock = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
            }
        }
        [System.Serializable]
        public struct WindowTheme
        {
            public HeaderTheme Header;
            public ContentTheme Content;

            public void Initialize()
            {
                Header.Initialize();
                Content.Initialize();
            }
        }
        [System.Serializable]
        public struct HeaderTheme
        {
            public Color Background;
            public TextTheme Text;

            public void Initialize()
            {
                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleCenter;
                Text.Font.fontStyle = FontStyle.Bold;

                Background = new Color(41, 41, 41, 255) / 255.0f;
            }
        }
        [System.Serializable]
        public struct ContentTheme
        {
            public Color Background;
            public TextTheme Text;
            public ThemeBackgroundAndText Title;
            public ToggleTheme Toggle;
            public DropdownTheme Dropdown;
            public InputFieldTheme Inputfield;
            public ListTheme List;
            public ButtonTheme MainButton;
            public ButtonTheme SecondaryButton;

            public void Initialize()
            {
                Background = new Color(56, 56, 56, 255) / 255.0f;

                Title.Background = new Color(80, 80, 80, 255) / 255.0f;
                Title.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Title.Text.Font = FontData.defaultFontData;
                Title.Text.Font.fontSize = 14;
                Title.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Title.Text.Font.alignByGeometry = true;
                Title.Text.Font.alignment = TextAnchor.MiddleLeft;
                Title.Text.Font.fontStyle = FontStyle.Normal;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;

                Dropdown.ColorBlock = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Dropdown.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleLeft;
                Text.Font.fontStyle = FontStyle.Normal;

                Inputfield.ColorBlock = ColorBlock.defaultColorBlock;
                Inputfield.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Inputfield.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Inputfield.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Inputfield.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;

                List.ScrollBar.ColorBlock = ColorBlock.defaultColorBlock;
                List.ScrollBar.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                List.ScrollBar.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                List.ScrollBar.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                List.ScrollBar.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                List.ScrollBar.Background = new Color(40, 40, 40, 255) / 255.0f;
                List.Background = new Color(80, 80, 80, 255) / 255.0f;


                MainButton.Text.Font = FontData.defaultFontData;
                MainButton.Text.Font.fontSize = 14;
                MainButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                MainButton.Text.Font.alignByGeometry = true;
                MainButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                MainButton.Text.Font.fontStyle = FontStyle.Bold;
                MainButton.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                MainButton.ColorBlock = ColorBlock.defaultColorBlock;
                MainButton.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                MainButton.ColorBlock.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                MainButton.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                MainButton.ColorBlock.disabledColor = new Color(200, 200, 200, 128) / 255.0f;

                SecondaryButton.Text.Font = FontData.defaultFontData;
                SecondaryButton.Text.Font.fontSize = 14;
                SecondaryButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                SecondaryButton.Text.Font.alignByGeometry = true;
                SecondaryButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                SecondaryButton.Text.Font.fontStyle = FontStyle.Normal;
                SecondaryButton.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                SecondaryButton.ColorBlock = ColorBlock.defaultColorBlock;
                SecondaryButton.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                SecondaryButton.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                SecondaryButton.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                SecondaryButton.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
            }
        }
        [System.Serializable]
        public struct ToolbarTheme
        {
            public Color Background;
            public TextTheme Text;
            public ButtonTheme Button;
            public ToggleTheme Toggle;
            public InputFieldTheme InputField;
            public SliderTheme Slider;
            public DropdownTheme DropdownText;
            public DropdownTheme DropdownImage;

            public void Initialize()
            {
                Background = new Color(40, 40, 40, 255) / 255.0f;
                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 14;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                Button.Text.Font.fontStyle = FontStyle.Normal;
                Button.Text.Color = new Color(0, 0, 0, 255) / 255.0f;

                InputField.ColorBlock = ColorBlock.defaultColorBlock;
                InputField.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                InputField.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                InputField.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                InputField.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleCenter;
                Text.Font.fontStyle = FontStyle.Normal;

                Slider.Background = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Fill = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Handle = new Color(170, 170, 170, 255) / 255.0f;

                DropdownText.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownText.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownText.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownText.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;

                DropdownImage.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownImage.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownImage.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownImage.ColorBlock.disabledColor = new Color(255, 255, 255, 255) / 255.0f;
            }
        }

        #region Structs
        [System.Serializable]
        public struct ThemeBackgroundAndText
        {
            public Color Background;  
            public TextTheme Text;
        }
        [System.Serializable]
        public struct VisualizationTheme
        {
            public Color Background;
            public Color AlternativeBackground;
            public Color SwapBackground;
            public TextTheme Text;
            public ButtonTheme Button;
            public ToggleTheme Toggle;
            public InputFieldTheme InputField;
            public SliderTheme Slider;
            public DropdownTheme Dropdown;
            public ViewTheme View;

            public void Initialize()
            {
                View.Initialize();
                
                Background = new Color(40, 40, 40, 255) / 255.0f;
                AlternativeBackground = new Color(60, 60, 60, 255) / 255.0f;
                SwapBackground = new Color(50, 50, 255, 150) / 255.0f;

                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 12;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                Button.Text.Font.fontStyle = FontStyle.Normal;
                Button.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                InputField.ColorBlock = ColorBlock.defaultColorBlock;
                InputField.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                InputField.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                InputField.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                InputField.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 12;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleCenter;
                Text.Font.fontStyle = FontStyle.Normal;

                Slider.Background = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Fill = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Handle = new Color(170, 170, 170, 255) / 255.0f;

                Dropdown.ColorBlock = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
            }
        }
        [System.Serializable]
        public struct ViewTheme
        {
            public Color Normal;
            public Color Selected;
            public Color Clicked;

            public void Initialize()
            {
                Normal = new Color(212, 212, 212, 255) / 255.0f;
                Selected = new Color(156, 187, 227, 255) / 255.0f;
                Clicked = new Color(59, 122, 194, 255) / 255.0f;
            }
        }
        [System.Serializable]
        public struct SliderTheme
        {
            public Color Background;
            public Color Fill;
            public Color Handle;
        }
        [System.Serializable]
        public struct ToggleTheme
        {
            public ColorBlock ColorBlock;
            public Color Checkmark;
        }
        [System.Serializable]
        public struct ButtonTheme
        {
            public ColorBlock ColorBlock;
            public TextTheme Text;
        }
        [System.Serializable]
        public struct InputFieldTheme
        {
            public ColorBlock ColorBlock;
            public TextTheme Text;
        }
        [System.Serializable]
        public struct ScrollBarTheme
        {
            public Color Background;
            public ColorBlock ColorBlock;
        }
        [System.Serializable]
        public struct ListTheme
        {
            public Color Background;
            public ScrollBarTheme ScrollBar;
        }
        [System.Serializable]
        public struct DropdownTheme
        {
            public ColorBlock ColorBlock;
        }
        [System.Serializable]
        public struct TextTheme
        {
            public FontData Font;
            public Color Color;
        }

        [System.Serializable]
        public struct ColorBlockTheme
        {
            public ColorBlock ColorBlock;
        }

        [System.Serializable]
        public struct FontDataTheme
        {
            public FontData FontData;
        }
    }
    #endregion
}