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

        public void SetDefaultValues()
        {
            General.SetDefaultValues();
            Menu.SetDefaultValues();
            Window.SetDefaultValues();
            Toolbar.SetDefaultValues();
            Visualization.SetDefaultValues();
        }

        [System.Serializable]
        public class GeneralTheme
        {
            public Color Error = new Color();
            public Color OK = new Color();
            public Color NotInteractable = new Color();
            public Color TooltipBackground;
            public TextTheme TooltipText;

            public void SetDefaultValues()
            {
                NotInteractable = new Color(100, 100, 100, 255) / 255.0f;
                OK = new Color(50, 200, 50, 255) / 255.0f;
                Error = new Color(200, 50, 50, 255) / 255.0f;

                TooltipBackground = new Color(40, 40, 40, 255) / 255.0f;
                TooltipText.Color = new Color(255, 255, 255, 255) / 255.0f;
                TooltipText.Font = FontData.defaultFontData;
                TooltipText.Font.fontSize = 12;
                TooltipText.Font.font = Resources.Load<Font>("Fonts/Arial");
                TooltipText.Font.alignByGeometry = true;
                TooltipText.Font.fontStyle = FontStyle.Normal;
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

            public void SetDefaultValues()
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

            public void SetDefaultValues()
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
            public ButtonTheme Button;

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

                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 14;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                Button.Text.Font.fontStyle = FontStyle.Bold;
                Button.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
            }
        }
        [System.Serializable]
        public struct ContentTheme
        {
            public Color Background;
            public TextTheme Text;
            public TitleTheme Title;
            public ToggleTheme Toggle;
            public DropdownTheme Dropdown;
            public InputFieldTheme Inputfield;
            public ScrollRectTheme ScrollRect;
            public ButtonTheme MainButton;
            public ButtonTheme SecondaryButton;
            public ItemTheme Item;

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

                ScrollRect.Scrollbar.ColorBlock = ColorBlock.defaultColorBlock;
                ScrollRect.Scrollbar.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                ScrollRect.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                ScrollRect.Background = new Color(80, 80, 80, 255) / 255.0f;

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

                Item.Initialize();
            }
        }
        [System.Serializable]
        public struct ToolbarTheme
        {
            public Color Background;
            public Color MainEvent;
            public Color SecondaryEvent;
            public TextTheme Text;
            public TextTheme SecondaryText;
            public TextTheme TimelineText;
            public ButtonTheme ButtonImage;
            public ButtonTheme ButtonText;
            public ToggleTheme Toggle;
            public InputFieldTheme InputField;
            public SliderTheme Slider;
            public DropdownTheme DropdownText;
            public DropdownTheme DropdownImage;
            public DropdownTheme DropdownTextWithIcon;

            public void SetDefaultValues()
            {
                Background = new Color(40, 40, 40, 255) / 255.0f;
                MainEvent = new Color(255, 0, 0, 150) / 255.0f;
                SecondaryEvent = new Color(72, 0, 255, 150) / 255.0f;

                ButtonImage.ColorBlock = ColorBlock.defaultColorBlock;
                ButtonImage.ColorBlock.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonImage.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                ButtonImage.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                ButtonImage.ColorBlock.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonImage.Text.Font = FontData.defaultFontData;
                ButtonImage.Text.Font.fontSize = 14;
                ButtonImage.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                ButtonImage.Text.Font.alignByGeometry = true;
                ButtonImage.Text.Font.fontStyle = FontStyle.Normal;
                ButtonImage.Text.Color = new Color(0, 0, 0, 255) / 255.0f;

                ButtonText.ColorBlock = ColorBlock.defaultColorBlock;
                ButtonText.ColorBlock.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonText.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                ButtonText.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                ButtonText.ColorBlock.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonText.Text.Font = FontData.defaultFontData;
                ButtonText.Text.Font.fontSize = 14;
                ButtonText.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                ButtonText.Text.Font.alignByGeometry = true;
                ButtonText.Text.Font.fontStyle = FontStyle.Normal;
                ButtonText.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                InputField.ColorBlock = ColorBlock.defaultColorBlock;
                InputField.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                InputField.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                InputField.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                InputField.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                InputField.Text.Font = FontData.defaultFontData;
                InputField.Text.Font.fontSize = 14;
                InputField.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.Text.Font.alignByGeometry = true;
                InputField.Text.Font.fontStyle = FontStyle.Normal;
                InputField.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 14;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.fontStyle = FontStyle.Normal;

                SecondaryText.Color = new Color(126, 186, 255, 255) / 255.0f;
                SecondaryText.Font = FontData.defaultFontData;
                SecondaryText.Font.fontSize = 14;
                SecondaryText.Font.font = Resources.Load<Font>("Fonts/Arial");
                SecondaryText.Font.alignByGeometry = true;
                SecondaryText.Font.fontStyle = FontStyle.Normal;

                TimelineText.Color = new Color(50, 50, 50, 255) / 255.0f;
                TimelineText.Font = FontData.defaultFontData;
                TimelineText.Font.fontSize = 14;
                TimelineText.Font.font = Resources.Load<Font>("Fonts/Arial");
                TimelineText.Font.alignByGeometry = true;
                TimelineText.Font.fontStyle = FontStyle.Bold;

                Slider.Background = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Fill = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Handle = new Color(170, 170, 170, 255) / 255.0f;

                DropdownText.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownText.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownText.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownText.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Text.Font = FontData.defaultFontData;
                DropdownText.Text.Font.fontSize = 14;
                DropdownText.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownText.Text.Font.alignByGeometry = true;
                DropdownText.Text.Font.fontStyle = FontStyle.Normal;
                DropdownText.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownText.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownText.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownText.Template.Scrollbar.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                DropdownText.Item.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownText.Item.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.Item.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownText.Item.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownText.Item.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                DropdownText.Item.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Item.Text.Font = FontData.defaultFontData;
                DropdownText.Item.Text.Font.fontSize = 14;
                DropdownText.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownText.Item.Text.Font.alignByGeometry = true;
                DropdownText.Item.Text.Font.fontStyle = FontStyle.Normal;

                DropdownImage.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownImage.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownImage.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownImage.ColorBlock.disabledColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Text.Font = FontData.defaultFontData;
                DropdownImage.Text.Font.fontSize = 14;
                DropdownImage.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownImage.Text.Font.alignByGeometry = true;
                DropdownImage.Text.Font.fontStyle = FontStyle.Normal;
                DropdownImage.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownImage.Template.Scrollbar.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                DropdownImage.Item.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownImage.Item.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Item.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownImage.Item.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownImage.Item.ColorBlock.disabledColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;

                DropdownTextWithIcon.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownTextWithIcon.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownTextWithIcon.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownTextWithIcon.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                DropdownTextWithIcon.Text.Font = FontData.defaultFontData;
                DropdownTextWithIcon.Text.Font.fontSize = 14;
                DropdownTextWithIcon.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownTextWithIcon.Text.Font.alignByGeometry = true;
                DropdownTextWithIcon.Text.Font.fontStyle = FontStyle.Bold;
                DropdownTextWithIcon.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock = ColorBlock.defaultColorBlock;
                DropdownTextWithIcon.Item.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                DropdownTextWithIcon.Item.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownTextWithIcon.Item.Text.Font = FontData.defaultFontData;
                DropdownTextWithIcon.Item.Text.Font.fontSize = 14;
                DropdownTextWithIcon.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownTextWithIcon.Item.Text.Font.alignByGeometry = true;
                DropdownTextWithIcon.Item.Text.Font.fontStyle = FontStyle.Normal;
            }
        }

        [System.Serializable]
        public struct ItemTheme
        {
            public TextTheme Text;
            public ToggleTheme Toggle;
            public ButtonTheme Button;

            public void Initialize()
            {
                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleLeft;
                Text.Font.fontStyle = FontStyle.Normal;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;

                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 14;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                Button.Text.Font.fontStyle = FontStyle.Bold;
                Button.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(255, 255, 255, 0) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(200, 200, 200, 0) / 255.0f;
            }
        }
        #region Structs
        [System.Serializable]
        public struct TitleTheme
        {
            public Color Background;  
            public TextTheme Text;
        }
        [System.Serializable]
        public struct VisualizationTheme
        {
            public Color Background;
            public Color SwapBackground;
            public Color TransparentBackground;
            public TextTheme Text;
            public TextTheme SiteText;
            public TextTheme MarsAtlasText;
            public TextTheme BroadmanText;
            public ButtonTheme Button;
            public ButtonTheme InvisibleButton;
            public ToggleTheme Toggle;
            public InputFieldTheme Inputfield;
            public SliderTheme Slider;
            public DropdownTheme Dropdown;
            public ViewTheme View;

            public void SetDefaultValues()
            {
                View.Initialize();

                Background = new Color(50, 50, 50, 255) / 255.0f;
                SwapBackground = new Color(50, 50, 255, 150) / 255.0f;
                TransparentBackground = new Color(50, 50, 50, 150) / 255.0f;

                Button.ColorBlock = ColorBlock.defaultColorBlock;
                Button.ColorBlock.normalColor = new Color(0, 0, 0, 150) / 255.0f;
                Button.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Button.ColorBlock.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.disabledColor = new Color(0, 0, 0, 150) / 255.0f;
                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 12;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.fontStyle = FontStyle.Normal;
                Button.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                InvisibleButton.ColorBlock = ColorBlock.defaultColorBlock;
                InvisibleButton.ColorBlock.normalColor = new Color(0, 0, 0, 150) / 255.0f;
                InvisibleButton.ColorBlock.highlightedColor = new Color(0, 0, 0, 255) / 255.0f;
                InvisibleButton.ColorBlock.pressedColor = new Color(0, 0, 0, 150) / 255.0f;
                InvisibleButton.ColorBlock.disabledColor = new Color(0, 0, 0, 150) / 255.0f;
                InvisibleButton.Text.Font = FontData.defaultFontData;
                InvisibleButton.Text.Font.fontSize = 14;
                InvisibleButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                InvisibleButton.Text.Font.alignByGeometry = true;
                InvisibleButton.Text.Font.fontStyle = FontStyle.Normal;
                InvisibleButton.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                Inputfield.ColorBlock = ColorBlock.defaultColorBlock;
                Inputfield.ColorBlock.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Inputfield.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Inputfield.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Inputfield.ColorBlock.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Inputfield.Text.Font = FontData.defaultFontData;
                Inputfield.Text.Font.fontSize = 12;
                Inputfield.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Inputfield.Text.Font.alignByGeometry = true;
                Inputfield.Text.Font.fontStyle = FontStyle.Normal;
                Inputfield.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                Toggle.ColorBlock = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 12;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 12;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.fontStyle = FontStyle.Normal;

                SiteText.Color = new Color(105, 242, 242, 255) / 255.0f;
                SiteText.Font = FontData.defaultFontData;
                SiteText.Font.fontSize = 12;
                SiteText.Font.font = Resources.Load<Font>("Fonts/Arial");
                SiteText.Font.alignByGeometry = true;
                SiteText.Font.fontStyle = FontStyle.Normal;

                MarsAtlasText.Color = new Color(234, 198, 47, 255) / 255.0f;
                MarsAtlasText.Font = FontData.defaultFontData;
                MarsAtlasText.Font.fontSize = 12;
                MarsAtlasText.Font.font = Resources.Load<Font>("Fonts/Arial");
                MarsAtlasText.Font.alignByGeometry = true;
                MarsAtlasText.Font.fontStyle = FontStyle.Normal;

                BroadmanText.Color = new Color(87, 233, 100, 255) / 255.0f;
                BroadmanText.Font = FontData.defaultFontData;
                BroadmanText.Font.fontSize = 12;
                BroadmanText.Font.font = Resources.Load<Font>("Fonts/Arial");
                BroadmanText.Font.alignByGeometry = true;
                BroadmanText.Font.fontStyle = FontStyle.Normal;

                Slider.Background = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Fill = new Color(255, 255, 255, 255) / 255.0f;
                Slider.Handle = new Color(170, 170, 170, 255) / 255.0f;

                Dropdown.ColorBlock = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Text.Font = FontData.defaultFontData;
                Dropdown.Text.Font.fontSize = 14;
                Dropdown.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Text.Font.alignByGeometry = true;
                Dropdown.Text.Font.fontStyle = FontStyle.Normal;
                Dropdown.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                Dropdown.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                Dropdown.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock = ColorBlock.defaultColorBlock;
                Dropdown.Template.Scrollbar.ColorBlock.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                Dropdown.Item.ColorBlock = ColorBlock.defaultColorBlock;
                Dropdown.Item.ColorBlock.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Item.ColorBlock.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.Item.ColorBlock.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.Item.ColorBlock.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Dropdown.Item.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Item.Text.Font = FontData.defaultFontData;
                Dropdown.Item.Text.Font.fontSize = 14;
                Dropdown.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Item.Text.Font.alignByGeometry = true;
                Dropdown.Item.Text.Font.fontStyle = FontStyle.Normal;
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

        #region Structs
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
            public TextTheme Text;
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
            public Color Handle;
        }
        [System.Serializable]
        public struct ScrollRectTheme
        {
            public Color Background;
            public ScrollBarTheme Scrollbar;
        }
        [System.Serializable]
        public struct DropdownTheme
        {
            public ColorBlock ColorBlock;
            public TextTheme Text;
            public Color ArrowColor;
            public ScrollRectTheme Template;
            public ToggleTheme Item;
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
    #endregion
}