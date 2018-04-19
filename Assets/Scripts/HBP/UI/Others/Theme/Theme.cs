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

        public static void UpdateThemeElements(Theme theme)
        {
            foreach (OldThemeElement element in FindObjectsOfType<OldThemeElement>())
            {
                element.Set(theme);
            }
        }

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
            public Color Warning = new Color();
            public Color OK = new Color();
            public Color NotInteractable = new Color();
            public Color TooltipBackground;
            public TextTheme TooltipText;
            public ImageTheme Background;
            public LoadingCircleTheme LoadingCircle;
            public DialogBoxTheme DialogBox;
            public CursorTheme Cursor;
            public CursorTheme LeftRightCursor;
            public CursorTheme TopBottomCursor;
            public CursorTheme BottomLeftTopRightCursor;
            public CursorTheme TopLeftBottomRightCursor;
            public CursorTheme MoveCursor;

            public void SetDefaultValues()
            {
                NotInteractable = new Color(100, 100, 100, 255) / 255.0f;
                OK = new Color(50, 200, 50, 255) / 255.0f;
                Warning = new Color(200, 200, 50, 255) / 255.0f;
                Error = new Color(200, 50, 50, 255) / 255.0f;
                Background.Color = new Color(20, 20, 20, 255) / 255.0f;

                TooltipBackground = new Color(40, 40, 40, 255) / 255.0f;
                TooltipText.Color = new Color(255, 255, 255, 255) / 255.0f;
                TooltipText.Font = FontData.defaultFontData;
                TooltipText.Font.fontSize = 12;
                TooltipText.Font.font = Resources.Load<Font>("Fonts/Arial");
                TooltipText.Font.alignByGeometry = true;
                TooltipText.Font.fontStyle = FontStyle.Normal;

                LoadingCircle.Background = new Color(40, 40, 40, 255) / 255.0f;
                LoadingCircle.Circle = new Color(60, 60, 60, 255) / 255.0f;
                LoadingCircle.Fill = new Color(59, 122, 194, 255) / 255.0f;
                LoadingCircle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                LoadingCircle.Text.Font = FontData.defaultFontData;
                LoadingCircle.Text.Font.fontSize = 14;
                LoadingCircle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                LoadingCircle.Text.Font.alignByGeometry = false;
                LoadingCircle.Text.Font.fontStyle = FontStyle.Normal;

                DialogBox.Initialize();
            }
        }
        [System.Serializable]
        public struct MenuTheme
        {
            public ImageTheme Background;
            public ImageTheme SubMenuBackground;
            public TextTheme Text;
            public ButtonTheme Button;
            public ToggleTheme Toggle;
            public DropdownTheme Dropdown;

            public void SetDefaultValues()
            {
                Background.Color = new Color(40, 40, 40, 255) / 255.0f;
                SubMenuBackground.Color = new Color(20, 20, 20, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 13;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.fontStyle = FontStyle.Normal;

                Button.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Button.ColorBlock.Colors.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.ColorBlock.Colors.highlightedColor = new Color(30, 30, 30, 255) / 255.0f;
                Button.ColorBlock.Colors.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.Colors.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                Button.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 13;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.fontStyle = FontStyle.Normal;
                Button.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                Button.DisabledText.Font = FontData.defaultFontData;
                Button.DisabledText.Font.fontSize = 13;
                Button.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.DisabledText.Font.alignByGeometry = true;
                Button.DisabledText.Font.fontStyle = FontStyle.Normal;
                Button.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Button.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                Toggle.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 13;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;
                Toggle.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                Toggle.DisabledText.Font = FontData.defaultFontData;
                Toggle.DisabledText.Font.fontSize = 13;
                Toggle.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.DisabledText.Font.alignByGeometry = true;
                Toggle.DisabledText.Font.fontStyle = FontStyle.Normal;
                Toggle.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                Dropdown.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Text.Font = FontData.defaultFontData;
                Dropdown.Text.Font.fontSize = 13;
                Dropdown.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Text.Font.alignByGeometry = true;
                Dropdown.Text.Font.fontStyle = FontStyle.Normal;
                Dropdown.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                Dropdown.DisabledText.Font = FontData.defaultFontData;
                Dropdown.DisabledText.Font.fontSize = 13;
                Dropdown.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.DisabledText.Font.alignByGeometry = true;
                Dropdown.DisabledText.Font.fontStyle = FontStyle.Normal;
                Dropdown.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
            }
        }
        [System.Serializable]
        public struct WindowTheme
        {
            public HeaderTheme Header;
            public ContentTheme Content;
            public BorderEffectTheme Selector;
            public BorderEffectTheme Shadow;

            public void SetDefaultValues()
            {
                Header.Initialize();
                Content.Initialize();
            }
        }
        [System.Serializable]
        public struct HeaderTheme
        {
            public ImageTheme Background;
            public TextTheme Text;
            public ButtonTheme CloseButton;
            public ButtonTheme MaximizeButton;
            public ButtonTheme MinimizeButton;

            public void Initialize()
            {
                Background.Color = new Color(40, 40, 40, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleCenter;
                Text.Font.fontStyle = FontStyle.Bold;

                CloseButton.Text.Font = FontData.defaultFontData;
                CloseButton.Text.Font.fontSize = 14;
                CloseButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                CloseButton.Text.Font.alignByGeometry = true;
                CloseButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                CloseButton.Text.Font.fontStyle = FontStyle.Bold;
                CloseButton.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                CloseButton.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                CloseButton.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                CloseButton.ColorBlock.Colors.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                CloseButton.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                CloseButton.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;

                MaximizeButton.Text.Font = FontData.defaultFontData;
                MaximizeButton.Text.Font.fontSize = 14;
                MaximizeButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                MaximizeButton.Text.Font.alignByGeometry = true;
                MaximizeButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                MaximizeButton.Text.Font.fontStyle = FontStyle.Bold;
                MaximizeButton.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                MaximizeButton.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                MaximizeButton.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                MaximizeButton.ColorBlock.Colors.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                MaximizeButton.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                MaximizeButton.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;

                MinimizeButton.Text.Font = FontData.defaultFontData;
                MinimizeButton.Text.Font.fontSize = 14;
                MinimizeButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                MinimizeButton.Text.Font.alignByGeometry = true;
                MinimizeButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                MinimizeButton.Text.Font.fontStyle = FontStyle.Bold;
                MinimizeButton.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                MinimizeButton.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                MinimizeButton.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                MinimizeButton.ColorBlock.Colors.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                MinimizeButton.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                MinimizeButton.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
            }
        }
        [System.Serializable]
        public struct ContentTheme
        {
            public ImageTheme Background;
            public TextTheme Text;
            public TitleTheme Title;
            public ToggleTheme Toggle;
            public DropdownTheme Dropdown;
            public InputFieldTheme InputField;
            public FileSelectorTheme FileSelector;
            public ScrollRectTheme ScrollRect;
            public ButtonTheme MainButton;
            public ButtonTheme SecondaryButton;
            public ItemTheme Item;

            public void Initialize()
            {
                Background.Color = new Color(56, 56, 56, 255) / 255.0f;

                Title.Background.Color = new Color(80, 80, 80, 255) / 255.0f;
                Title.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Title.Text.Font = FontData.defaultFontData;
                Title.Text.Font.fontSize = 14;
                Title.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Title.Text.Font.alignByGeometry = true;
                Title.Text.Font.alignment = TextAnchor.MiddleLeft;
                Title.Text.Font.fontStyle = FontStyle.Normal;

                Toggle.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.Colors.disabledColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 14;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.alignment = TextAnchor.MiddleLeft;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;
                Toggle.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.DisabledText.Font = FontData.defaultFontData;
                Toggle.DisabledText.Font.fontSize = 14;
                Toggle.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.DisabledText.Font.alignByGeometry = true;
                Toggle.DisabledText.Font.alignment = TextAnchor.MiddleLeft;
                Toggle.DisabledText.Font.fontStyle = FontStyle.Normal;
                Toggle.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.DisabledIcon = new Color(255, 255, 255, 255) / 255.0f;

                Dropdown.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Dropdown.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Text.Font = FontData.defaultFontData;
                Dropdown.Text.Font.fontSize = 14;
                Dropdown.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Text.Font.alignByGeometry = true;
                Dropdown.Text.Font.alignment = TextAnchor.MiddleLeft;
                Dropdown.Text.Font.fontStyle = FontStyle.Normal;
                Dropdown.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.DisabledText.Font = FontData.defaultFontData;
                Dropdown.DisabledText.Font.fontSize = 14;
                Dropdown.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.DisabledText.Font.alignByGeometry = true;
                Dropdown.DisabledText.Font.alignment = TextAnchor.MiddleLeft;
                Dropdown.DisabledText.Font.fontStyle = FontStyle.Normal;
                Dropdown.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.DisabledIcon = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Arrow = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.DisabledArrow = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Template.Background = new Color(65, 65, 65, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Dropdown.Item.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Dropdown.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Dropdown.Item.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Item.Text.Font = FontData.defaultFontData;
                Dropdown.Item.Text.Font.fontSize = 14;
                Dropdown.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Item.Text.Font.alignByGeometry = true;
                Dropdown.Item.Text.Font.alignment = TextAnchor.MiddleLeft;
                Dropdown.Item.Text.Font.fontStyle = FontStyle.Normal;
                Dropdown.Item.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Item.DisabledText.Font = FontData.defaultFontData;
                Dropdown.Item.DisabledText.Font.fontSize = 14;
                Dropdown.Item.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Item.DisabledText.Font.alignByGeometry = true;
                Dropdown.Item.DisabledText.Font.alignment = TextAnchor.MiddleLeft;
                Dropdown.Item.DisabledText.Font.fontStyle = FontStyle.Normal;
                Dropdown.Item.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Item.DisabledIcon = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Dropdown.Template.Scrollbar.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.Template.Scrollbar.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Dropdown.Template.Scrollbar.Handle = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                Dropdown.Template.Background = new Color(80, 80, 80, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleLeft;
                Text.Font.fontStyle = FontStyle.Normal;

                FileSelector.InputField.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                FileSelector.InputField.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                FileSelector.InputField.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                FileSelector.InputField.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                FileSelector.InputField.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                FileSelector.InputField.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                FileSelector.InputField.Text.Font = FontData.defaultFontData;
                FileSelector.InputField.Text.Font.fontSize = 14;
                FileSelector.InputField.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                FileSelector.InputField.Text.Font.alignByGeometry = true;
                FileSelector.InputField.Text.Font.alignment = TextAnchor.MiddleLeft;
                FileSelector.InputField.Text.Font.fontStyle = FontStyle.Normal;
                FileSelector.InputField.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                FileSelector.InputField.DisabledText.Font = FontData.defaultFontData;
                FileSelector.InputField.DisabledText.Font.fontSize = 14;
                FileSelector.InputField.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                FileSelector.InputField.DisabledText.Font.alignByGeometry = true;
                FileSelector.InputField.DisabledText.Font.alignment = TextAnchor.MiddleLeft;
                FileSelector.InputField.DisabledText.Font.fontStyle = FontStyle.Normal;
                FileSelector.InputField.Icon = new Color(255, 255, 255, 255) / 255.0f;
                FileSelector.InputField.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                FileSelector.Button.Text.Font = FontData.defaultFontData;
                FileSelector.Button.Text.Font.fontSize = 14;
                FileSelector.Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                FileSelector.Button.Text.Font.alignByGeometry = true;
                FileSelector.Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                FileSelector.Button.Text.Font.fontStyle = FontStyle.Bold;
                FileSelector.Button.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                FileSelector.Button.DisabledText.Font = FontData.defaultFontData;
                FileSelector.Button.DisabledText.Font.fontSize = 14;
                FileSelector.Button.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                FileSelector.Button.DisabledText.Font.alignByGeometry = true;
                FileSelector.Button.DisabledText.Font.alignment = TextAnchor.MiddleCenter;
                FileSelector.Button.DisabledText.Font.fontStyle = FontStyle.Bold;
                FileSelector.Button.DisabledText.Color = new Color(80, 80, 80, 255) / 255.0f;
                FileSelector.Button.Icon = new Color(255, 255, 255, 255) / 255.0f;
                FileSelector.Button.DisabledIcon = new Color(80, 80, 80, 255) / 255.0f;
                FileSelector.Button.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                FileSelector.Button.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                FileSelector.Button.ColorBlock.Colors.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                FileSelector.Button.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                FileSelector.Button.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;

                InputField.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                InputField.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                InputField.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                InputField.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                InputField.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                InputField.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                InputField.Text.Font = FontData.defaultFontData;
                InputField.Text.Font.fontSize = 14;
                InputField.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.Text.Font.alignByGeometry = true;
                InputField.Text.Font.alignment = TextAnchor.MiddleLeft;
                InputField.Text.Font.fontStyle = FontStyle.Normal;
                InputField.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                InputField.DisabledText.Font = FontData.defaultFontData;
                InputField.DisabledText.Font.fontSize = 14;
                InputField.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.DisabledText.Font.alignByGeometry = true;
                InputField.DisabledText.Font.alignment = TextAnchor.MiddleLeft;
                InputField.DisabledText.Font.fontStyle = FontStyle.Normal;

                ScrollRect.Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                ScrollRect.Scrollbar.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                ScrollRect.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                ScrollRect.Background = new Color(80, 80, 80, 255) / 255.0f;

                MainButton.Text.Font = FontData.defaultFontData;
                MainButton.Text.Font.fontSize = 14;
                MainButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                MainButton.Text.Font.alignByGeometry = true;
                MainButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                MainButton.Text.Font.fontStyle = FontStyle.Bold;
                MainButton.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                MainButton.DisabledText.Font = FontData.defaultFontData;
                MainButton.DisabledText.Font.fontSize = 14;
                MainButton.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                MainButton.DisabledText.Font.alignByGeometry = true;
                MainButton.DisabledText.Font.alignment = TextAnchor.MiddleCenter;
                MainButton.DisabledText.Font.fontStyle = FontStyle.Bold;
                MainButton.DisabledText.Color = new Color(80, 80, 80, 255) / 255.0f;
                MainButton.Icon = new Color(0, 0, 0, 255) / 255.0f;
                MainButton.DisabledIcon = new Color(80, 80, 80, 255) / 255.0f;
                MainButton.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                MainButton.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                MainButton.ColorBlock.Colors.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                MainButton.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                MainButton.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;

                SecondaryButton.Text.Font = FontData.defaultFontData;
                SecondaryButton.Text.Font.fontSize = 14;
                SecondaryButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                SecondaryButton.Text.Font.alignByGeometry = true;
                SecondaryButton.Text.Font.alignment = TextAnchor.MiddleCenter;
                SecondaryButton.Text.Font.fontStyle = FontStyle.Normal;
                SecondaryButton.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                SecondaryButton.DisabledText.Font = FontData.defaultFontData;
                SecondaryButton.DisabledText.Font.fontSize = 14;
                SecondaryButton.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                SecondaryButton.DisabledText.Font.alignByGeometry = true;
                SecondaryButton.DisabledText.Font.alignment = TextAnchor.MiddleCenter;
                SecondaryButton.DisabledText.Font.fontStyle = FontStyle.Normal;
                SecondaryButton.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                SecondaryButton.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                SecondaryButton.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                SecondaryButton.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                SecondaryButton.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                SecondaryButton.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                SecondaryButton.Icon = new Color(255, 255, 255, 255) / 255.0f;
                SecondaryButton.DisabledIcon = new Color(255, 255, 255, 255) / 255.0f;

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
            public ScrollRectTheme ScrollRect;

            public void SetDefaultValues()
            {
                Background = new Color(40, 40, 40, 255) / 255.0f;
                MainEvent = new Color(255, 0, 0, 150) / 255.0f;
                SecondaryEvent = new Color(72, 0, 255, 150) / 255.0f;

                ScrollRect.Background = new Color(40, 40, 40, 255) / 255.0f;
                ScrollRect.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                ScrollRect.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                ScrollRect.Scrollbar.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                ScrollRect.Scrollbar.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;

                ButtonImage.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                ButtonImage.ColorBlock.Colors.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonImage.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                ButtonImage.ColorBlock.Colors.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                ButtonImage.ColorBlock.Colors.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonImage.Text.Font = FontData.defaultFontData;
                ButtonImage.Text.Font.fontSize = 14;
                ButtonImage.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                ButtonImage.Text.Font.alignByGeometry = true;
                ButtonImage.Text.Font.fontStyle = FontStyle.Normal;
                ButtonImage.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                ButtonImage.Icon = new Color(255, 255, 255, 255) / 255.0f;
                ButtonImage.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                ButtonImage.DisabledText.Font = FontData.defaultFontData;
                ButtonImage.DisabledText.Font.fontSize = 14;
                ButtonImage.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                ButtonImage.DisabledText.Font.alignByGeometry = true;
                ButtonImage.DisabledText.Font.fontStyle = FontStyle.Normal;
                ButtonImage.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;

                ButtonText.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                ButtonText.ColorBlock.Colors.normalColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonText.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                ButtonText.ColorBlock.Colors.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                ButtonText.ColorBlock.Colors.disabledColor = new Color(40, 40, 40, 255) / 255.0f;
                ButtonText.Text.Font = FontData.defaultFontData;
                ButtonText.Text.Font.fontSize = 14;
                ButtonText.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                ButtonText.Text.Font.alignByGeometry = true;
                ButtonText.Text.Font.fontStyle = FontStyle.Normal;
                ButtonText.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                ButtonText.Icon = new Color(255, 255, 255, 255) / 255.0f;
                ButtonText.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                ButtonText.DisabledText.Font = FontData.defaultFontData;
                ButtonText.DisabledText.Font.fontSize = 14;
                ButtonText.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                ButtonText.DisabledText.Font.alignByGeometry = true;
                ButtonText.DisabledText.Font.fontStyle = FontStyle.Normal;
                ButtonText.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;

                InputField.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                InputField.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                InputField.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                InputField.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                InputField.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                InputField.Text.Font = FontData.defaultFontData;
                InputField.Text.Font.fontSize = 14;
                InputField.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.Text.Font.alignByGeometry = true;
                InputField.Text.Font.fontStyle = FontStyle.Normal;
                InputField.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                InputField.DisabledText.Font = FontData.defaultFontData;
                InputField.DisabledText.Font.fontSize = 14;
                InputField.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.DisabledText.Font.alignByGeometry = true;
                InputField.DisabledText.Font.fontStyle = FontStyle.Normal;
                InputField.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                InputField.Icon = new Color(255, 255, 255, 255) / 255.0f;
                InputField.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                Toggle.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 14;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;
                Toggle.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                Toggle.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                Toggle.DisabledText.Font = FontData.defaultFontData;
                Toggle.DisabledText.Font.fontSize = 14;
                Toggle.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.DisabledText.Font.alignByGeometry = true;
                Toggle.DisabledText.Font.fontStyle = FontStyle.Normal;

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

                Slider.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Slider.ColorBlock.Colors.normalColor = new Color(170, 170, 170, 255) / 255.0f;
                Slider.ColorBlock.Colors.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                Slider.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                Slider.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Slider.Background = new Color(255, 255, 255, 255) / 255.0f;
                Slider.DisabledBackground = new Color(100, 100, 100, 255) / 255.0f;
                Slider.Fill = new Color(255, 255, 255, 255) / 255.0f;
                Slider.DisabledFill = new Color(0, 0, 0, 0) / 255.0f;
                Slider.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Slider.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                Slider.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                Slider.Text.Font = FontData.defaultFontData;
                Slider.Text.Font.fontSize = 14;
                Slider.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Slider.Text.Font.alignByGeometry = true;
                Slider.Text.Font.fontStyle = FontStyle.Bold;
                Slider.DisabledText.Color = new Color(0, 0, 0, 0) / 255.0f;
                Slider.DisabledText.Font = FontData.defaultFontData;
                Slider.DisabledText.Font.fontSize = 14;
                Slider.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Slider.DisabledText.Font.alignByGeometry = true;
                Slider.DisabledText.Font.fontStyle = FontStyle.Normal;

                DropdownText.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownText.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownText.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownText.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Text.Font = FontData.defaultFontData;
                DropdownText.Text.Font.fontSize = 14;
                DropdownText.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownText.Text.Font.alignByGeometry = true;
                DropdownText.Text.Font.fontStyle = FontStyle.Normal;
                DropdownText.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                DropdownText.DisabledText.Font = FontData.defaultFontData;
                DropdownText.DisabledText.Font.fontSize = 14;
                DropdownText.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownText.DisabledText.Font.alignByGeometry = true;
                DropdownText.DisabledText.Font.fontStyle = FontStyle.Normal;
                DropdownText.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownText.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownText.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownText.Template.Scrollbar.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.Colors.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                DropdownText.Template.Scrollbar.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                DropdownText.Item.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownText.Item.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.Item.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownText.Item.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownText.Item.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownText.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                DropdownText.Item.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Item.Text.Font = FontData.defaultFontData;
                DropdownText.Item.Text.Font.fontSize = 14;
                DropdownText.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownText.Item.Text.Font.alignByGeometry = true;
                DropdownText.Item.Text.Font.fontStyle = FontStyle.Normal;
                DropdownText.Item.Icon = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.Item.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                DropdownText.Icon = new Color(255, 255, 255, 255) / 255.0f;
                DropdownText.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                DropdownImage.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownImage.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownImage.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownImage.ColorBlock.Colors.disabledColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Text.Font = FontData.defaultFontData;
                DropdownImage.Text.Font.fontSize = 14;
                DropdownImage.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownImage.Text.Font.alignByGeometry = true;
                DropdownImage.Text.Font.fontStyle = FontStyle.Normal;
                DropdownImage.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownImage.Template.Scrollbar.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.Colors.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                DropdownImage.Template.Scrollbar.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                DropdownImage.Item.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownImage.Item.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Item.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownImage.Item.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownImage.Item.ColorBlock.Colors.disabledColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                DropdownImage.Item.Icon = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.Item.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                DropdownImage.Icon = new Color(255, 255, 255, 255) / 255.0f;
                DropdownImage.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                DropdownTextWithIcon.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownTextWithIcon.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownTextWithIcon.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownTextWithIcon.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                DropdownTextWithIcon.Text.Font = FontData.defaultFontData;
                DropdownTextWithIcon.Text.Font.fontSize = 14;
                DropdownTextWithIcon.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownTextWithIcon.Text.Font.alignByGeometry = true;
                DropdownTextWithIcon.Text.Font.fontStyle = FontStyle.Bold;
                DropdownTextWithIcon.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                DropdownTextWithIcon.DisabledText.Font = FontData.defaultFontData;
                DropdownTextWithIcon.DisabledText.Font.fontSize = 14;
                DropdownTextWithIcon.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownTextWithIcon.DisabledText.Font.alignByGeometry = true;
                DropdownTextWithIcon.DisabledText.Font.fontStyle = FontStyle.Normal;
                DropdownTextWithIcon.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.Colors.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                DropdownTextWithIcon.Template.Scrollbar.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                DropdownTextWithIcon.Item.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                DropdownTextWithIcon.Item.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                DropdownTextWithIcon.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                DropdownTextWithIcon.Item.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                DropdownTextWithIcon.Item.Text.Font = FontData.defaultFontData;
                DropdownTextWithIcon.Item.Text.Font.fontSize = 14;
                DropdownTextWithIcon.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                DropdownTextWithIcon.Item.Text.Font.alignByGeometry = true;
                DropdownTextWithIcon.Item.Text.Font.fontStyle = FontStyle.Bold;
                DropdownTextWithIcon.Item.Icon = new Color(255, 255, 255, 255) / 255.0f;
                DropdownTextWithIcon.Item.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                DropdownTextWithIcon.Icon = new Color(255, 255, 255, 255) / 255.0f;
                DropdownTextWithIcon.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
            }
        }
        [System.Serializable]
        public struct ItemTheme
        {
            public TextTheme Text;
            public ToggleTheme Toggle;
            public ButtonTheme Button;
            public Color Background;
            public BlocTheme Bloc;
            public ScrollBarTheme Scrollbar;

            public void Initialize()
            {
                Background = new Color(50, 50, 50, 255) / 255.0f;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = true;
                Text.Font.alignment = TextAnchor.MiddleLeft;
                Text.Font.fontStyle = FontStyle.Normal;

                Toggle.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.Colors.normalColor = new Color(50, 50, 50, 255) / 255.0f;
                Toggle.ColorBlock.Colors.highlightedColor = new Color(40, 40, 40, 255) / 255.0f;
                Toggle.ColorBlock.Colors.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Toggle.ColorBlock.Colors.disabledColor = new Color(65, 65, 65, 255) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 14;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;

                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 14;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                Button.Text.Font.fontStyle = FontStyle.Bold;
                Button.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Button.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Button.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Button.ColorBlock.Colors.highlightedColor = new Color(0, 0, 0, 100) / 255.0f;
                Button.ColorBlock.Colors.pressedColor = new Color(0, 0, 0, 120) / 255.0f;
                Button.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 0) / 255.0f;

                Scrollbar.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Scrollbar.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                Scrollbar.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Scrollbar.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Scrollbar.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                Scrollbar.Handle = new Color(255, 255, 255, 255) / 255.0f;

                Bloc.Initialize();
            }
        }
        [System.Serializable]
        public struct VisualizationTheme
        {
            public Color Background;
            public Color SwapBackground;
            public Color TransparentBackground;
            public Color Border;
            public TextTheme Text;
            public TextTheme SiteText;
            public TextTheme MarsAtlasText;
            public TextTheme BroadmanText;
            public ButtonTheme Button;
            public ButtonTheme InvisibleButton;
            public ToggleTheme Toggle;
            public InputFieldTheme InputField;
            public SliderTheme Slider;
            public DropdownTheme Dropdown;
            public ViewTheme View;

            public void SetDefaultValues()
            {
                View.Initialize();

                Background = new Color(50, 50, 50, 255) / 255.0f;
                SwapBackground = new Color(50, 50, 255, 150) / 255.0f;
                TransparentBackground = new Color(50, 50, 50, 150) / 255.0f;
                Border = new Color(0, 0, 0, 255) / 255.0f;

                Button.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Button.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 150) / 255.0f;
                Button.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Button.ColorBlock.Colors.pressedColor = new Color(20, 20, 20, 255) / 255.0f;
                Button.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 150) / 255.0f;
                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 12;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.fontStyle = FontStyle.Normal;
                Button.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Button.DisabledText.Font = FontData.defaultFontData;
                Button.DisabledText.Font.fontSize = 12;
                Button.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.DisabledText.Font.alignByGeometry = true;
                Button.DisabledText.Font.fontStyle = FontStyle.Normal;
                Button.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                Button.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Button.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                InvisibleButton.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                InvisibleButton.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 150) / 255.0f;
                InvisibleButton.ColorBlock.Colors.highlightedColor = new Color(0, 0, 0, 255) / 255.0f;
                InvisibleButton.ColorBlock.Colors.pressedColor = new Color(0, 0, 0, 150) / 255.0f;
                InvisibleButton.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 150) / 255.0f;
                InvisibleButton.Text.Font = FontData.defaultFontData;
                InvisibleButton.Text.Font.fontSize = 14;
                InvisibleButton.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                InvisibleButton.Text.Font.alignByGeometry = true;
                InvisibleButton.Text.Font.fontStyle = FontStyle.Normal;
                InvisibleButton.Text.Color = new Color(255, 255, 255, 255) / 255.0f;

                InputField.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                InputField.ColorBlock.Colors.normalColor = new Color(65, 65, 65, 255) / 255.0f;
                InputField.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                InputField.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                InputField.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                InputField.Text.Font = FontData.defaultFontData;
                InputField.Text.Font.fontSize = 12;
                InputField.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.Text.Font.alignByGeometry = true;
                InputField.Text.Font.fontStyle = FontStyle.Normal;
                InputField.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                InputField.DisabledText.Font = FontData.defaultFontData;
                InputField.DisabledText.Font.fontSize = 12;
                InputField.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                InputField.DisabledText.Font.alignByGeometry = true;
                InputField.DisabledText.Font.fontStyle = FontStyle.Normal;
                InputField.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                InputField.Icon = new Color(255, 255, 255, 255) / 255.0f;
                InputField.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                Toggle.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Toggle.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Toggle.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Toggle.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Toggle.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Toggle.Text.Font = FontData.defaultFontData;
                Toggle.Text.Font.fontSize = 12;
                Toggle.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.Text.Font.alignByGeometry = true;
                Toggle.Text.Font.fontStyle = FontStyle.Normal;
                Toggle.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Toggle.DisabledText.Font = FontData.defaultFontData;
                Toggle.DisabledText.Font.fontSize = 12;
                Toggle.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Toggle.DisabledText.Font.alignByGeometry = true;
                Toggle.DisabledText.Font.fontStyle = FontStyle.Normal;
                Toggle.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;

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

                Slider.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Slider.ColorBlock.Colors.normalColor = new Color(170, 170, 170, 255) / 255.0f;
                Slider.ColorBlock.Colors.highlightedColor = new Color(245, 245, 245, 255) / 255.0f;
                Slider.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                Slider.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Slider.Background = new Color(255, 255, 255, 255) / 255.0f;
                Slider.DisabledBackground = new Color(100, 100, 100, 255) / 255.0f;
                Slider.Fill = new Color(255, 255, 255, 255) / 255.0f;
                Slider.DisabledFill = new Color(0, 0, 0, 0) / 255.0f;
                Slider.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Slider.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
                Slider.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                Slider.Text.Font = FontData.defaultFontData;
                Slider.Text.Font.fontSize = 12;
                Slider.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Slider.Text.Font.alignByGeometry = true;
                Slider.Text.Font.fontStyle = FontStyle.Bold;
                Slider.DisabledText.Color = new Color(0, 0, 0, 0) / 255.0f;
                Slider.DisabledText.Font = FontData.defaultFontData;
                Slider.DisabledText.Font.fontSize = 14;
                Slider.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Slider.DisabledText.Font.alignByGeometry = true;
                Slider.DisabledText.Font.fontStyle = FontStyle.Normal;

                Dropdown.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Dropdown.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Text.Font = FontData.defaultFontData;
                Dropdown.Text.Font.fontSize = 12;
                Dropdown.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Text.Font.alignByGeometry = true;
                Dropdown.Text.Font.fontStyle = FontStyle.Normal;
                Dropdown.DisabledText.Font = FontData.defaultFontData;
                Dropdown.DisabledText.Font.fontSize = 12;
                Dropdown.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.DisabledText.Font.alignByGeometry = true;
                Dropdown.DisabledText.Font.fontStyle = FontStyle.Normal;
                Dropdown.DisabledText.Color = new Color(100, 100, 100, 255) / 255.0f;
                Dropdown.Template.Background = new Color(40, 40, 40, 255) / 255.0f;
                Dropdown.Template.Scrollbar.Background = new Color(40, 40, 40, 255) / 255.0f;
                Dropdown.Template.Scrollbar.Handle = new Color(59, 122, 194, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Dropdown.Item.ColorBlock.Colors.normalColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Item.ColorBlock.Colors.highlightedColor = new Color(80, 80, 80, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors.pressedColor = new Color(60, 60, 60, 255) / 255.0f;
                Dropdown.Item.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 0) / 255.0f;
                Dropdown.Item.Checkmark = new Color(59, 122, 194, 255) / 255.0f;
                Dropdown.Item.Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Dropdown.Item.Text.Font = FontData.defaultFontData;
                Dropdown.Item.Text.Font.fontSize = 12;
                Dropdown.Item.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Dropdown.Item.Text.Font.alignByGeometry = true;
                Dropdown.Item.Text.Font.fontStyle = FontStyle.Normal;
            }
        }
        [System.Serializable]
        public struct TitleTheme
        {
            public ImageTheme Background;
            public TextTheme Text;
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
            public ColorBlockTheme ColorBlock;
            public Color Background;
            public Color DisabledBackground;
            public Color Fill;
            public Color DisabledFill;
            public Color DisabledIcon;
            public Color Icon;
            public TextTheme Text;
            public TextTheme DisabledText;
        }
        [System.Serializable]
        public struct ToggleTheme
        {
            public ImageTheme Background;
            public ColorBlockTheme ColorBlock;
            public Color Checkmark;
            public TextTheme Text;
            public TextTheme DisabledText;
            public Color Icon;
            public Color DisabledIcon;
        }
        [System.Serializable]
        public struct ButtonTheme
        {
            public ImageTheme Background;
            public ColorBlockTheme ColorBlock;
            public TextTheme Text;
            public TextTheme DisabledText;
            public Color Icon;
            public Color DisabledIcon;
        }
        [System.Serializable]
        public struct InputFieldTheme
        {
            public ImageTheme Background;
            public ColorBlockTheme ColorBlock;
            public TextTheme Text;
            public TextTheme DisabledText;
            public Color Icon;
            public Color DisabledIcon;
        }
        [System.Serializable]
        public struct FileSelectorTheme
        {
            public InputFieldTheme InputField;
            public ButtonTheme Button;
        }
        [System.Serializable]
        public struct ScrollBarTheme
        {
            public Color Background;
            public ColorBlockTheme ColorBlock;
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
            public ImageTheme Background;
            public ColorBlockTheme ColorBlock;
            public TextTheme Text;
            public TextTheme DisabledText;
            public Color Icon;
            public Color DisabledIcon;
            public Color Arrow;
            public Color DisabledArrow;
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
            public ColorBlock Colors;
        }
        [System.Serializable]
        public struct FontDataTheme
        {
            public FontData FontData;
        }
        [System.Serializable]
        public struct BlocTheme
        {
            public ButtonTheme Container;
            public ButtonTheme MainBloc;
            public ButtonTheme SecondaryBloc;

            public void Initialize()
            {
                Container.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Container.ColorBlock.Colors.normalColor = new Color(80, 80, 80, 255) / 255.0f;
                Container.ColorBlock.Colors.highlightedColor = new Color(70, 70, 70, 255) / 255.0f;
                Container.ColorBlock.Colors.pressedColor = new Color(50, 50, 50, 255) / 255.0f;
                Container.ColorBlock.Colors.disabledColor = new Color(150, 150, 150, 255) / 255.0f;
                Container.Text.Font = FontData.defaultFontData;
                Container.Text.Font.fontSize = 26;
                Container.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Container.Text.Font.alignByGeometry = true;
                Container.Text.Font.fontStyle = FontStyle.Bold;
                Container.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                Container.DisabledText.Font = FontData.defaultFontData;
                Container.DisabledText.Font.fontSize = 26;
                Container.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Container.DisabledText.Font.alignByGeometry = true;
                Container.DisabledText.Font.fontStyle = FontStyle.Bold;
                Container.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                Container.Icon = new Color(255, 255, 255, 255) / 255.0f;
                Container.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                MainBloc.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                MainBloc.ColorBlock.Colors.normalColor = new Color(0, 150, 0, 255) / 255.0f;
                MainBloc.ColorBlock.Colors.highlightedColor = new Color(0, 140, 0, 255) / 255.0f;
                MainBloc.ColorBlock.Colors.pressedColor = new Color(0, 120, 0, 255) / 255.0f;
                MainBloc.ColorBlock.Colors.disabledColor = new Color(0, 0, 0, 255) / 255.0f;
                MainBloc.Text.Font = FontData.defaultFontData;
                MainBloc.Text.Font.fontSize = 26;
                MainBloc.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                MainBloc.Text.Font.alignByGeometry = true;
                MainBloc.Text.Font.fontStyle = FontStyle.Bold;
                MainBloc.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                MainBloc.DisabledText.Font = FontData.defaultFontData;
                MainBloc.DisabledText.Font.fontSize = 26;
                MainBloc.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                MainBloc.DisabledText.Font.alignByGeometry = true;
                MainBloc.DisabledText.Font.fontStyle = FontStyle.Bold;
                MainBloc.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                MainBloc.Icon = new Color(255, 255, 255, 255) / 255.0f;
                MainBloc.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;

                SecondaryBloc.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                SecondaryBloc.ColorBlock.Colors.normalColor = new Color(150, 0, 0, 255) / 255.0f;
                SecondaryBloc.ColorBlock.Colors.highlightedColor = new Color(140, 0, 0, 255) / 255.0f;
                SecondaryBloc.ColorBlock.Colors.pressedColor = new Color(120, 0, 0, 255) / 255.0f;
                SecondaryBloc.ColorBlock.Colors.disabledColor = new Color(150, 0, 0, 255) / 255.0f;
                SecondaryBloc.Text.Font = FontData.defaultFontData;
                SecondaryBloc.Text.Font.fontSize = 26;
                SecondaryBloc.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                SecondaryBloc.Text.Font.alignByGeometry = true;
                SecondaryBloc.Text.Font.fontStyle = FontStyle.Bold;
                SecondaryBloc.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                SecondaryBloc.DisabledText.Font = FontData.defaultFontData;
                SecondaryBloc.DisabledText.Font.fontSize = 26;
                SecondaryBloc.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                SecondaryBloc.DisabledText.Font.alignByGeometry = true;
                SecondaryBloc.DisabledText.Font.fontStyle = FontStyle.Bold;
                SecondaryBloc.DisabledText.Color = new Color(255, 255, 255, 255) / 255.0f;
                SecondaryBloc.Icon = new Color(255, 255, 255, 255) / 255.0f;
                SecondaryBloc.DisabledIcon = new Color(100, 100, 100, 255) / 255.0f;
            }
        }
        [System.Serializable]
        public struct LoadingCircleTheme
        {
            public Color Background;
            public Color Circle;
            public Color Fill;
            public TextTheme Text;
        }
        [System.Serializable]
        public struct DialogBoxTheme
        {
            public ImageTheme Background;
            public TextTheme Title;
            public TextTheme Text;
            public ButtonTheme Button;

            public void Initialize()
            {
                Background.Color = new Color(40, 40, 40, 255) / 255.0f;

                Title.Color = new Color(255, 255, 255, 255) / 255.0f;
                Title.Font = FontData.defaultFontData;
                Title.Font.fontSize = 14;
                Title.Font.font = Resources.Load<Font>("Fonts/Arial");
                Title.Font.alignByGeometry = false;
                Title.Font.fontStyle = FontStyle.Bold;

                Text.Color = new Color(255, 255, 255, 255) / 255.0f;
                Text.Font = FontData.defaultFontData;
                Text.Font.fontSize = 14;
                Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Text.Font.alignByGeometry = false;
                Text.Font.fontStyle = FontStyle.Normal;

                Button.Text.Font = FontData.defaultFontData;
                Button.Text.Font.fontSize = 14;
                Button.Text.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.Text.Font.alignByGeometry = true;
                Button.Text.Font.alignment = TextAnchor.MiddleCenter;
                Button.Text.Font.fontStyle = FontStyle.Bold;
                Button.Text.Color = new Color(0, 0, 0, 255) / 255.0f;
                Button.DisabledText.Font = FontData.defaultFontData;
                Button.DisabledText.Font.fontSize = 14;
                Button.DisabledText.Font.font = Resources.Load<Font>("Fonts/Arial");
                Button.DisabledText.Font.alignByGeometry = true;
                Button.DisabledText.Font.alignment = TextAnchor.MiddleCenter;
                Button.DisabledText.Font.fontStyle = FontStyle.Bold;
                Button.DisabledText.Color = new Color(80, 80, 80, 255) / 255.0f;
                Button.Icon = new Color(0, 0, 0, 255) / 255.0f;
                Button.DisabledIcon = new Color(80, 80, 80, 255) / 255.0f;
                Button.ColorBlock.Colors = ColorBlock.defaultColorBlock;
                Button.ColorBlock.Colors.normalColor = new Color(255, 255, 255, 255) / 255.0f;
                Button.ColorBlock.Colors.highlightedColor = new Color(220, 220, 220, 255) / 255.0f;
                Button.ColorBlock.Colors.pressedColor = new Color(200, 200, 200, 255) / 255.0f;
                Button.ColorBlock.Colors.disabledColor = new Color(200, 200, 200, 128) / 255.0f;
            }
        }

        [System.Serializable]
        public struct CursorTheme
        {
            public Texture2D Texture;
            public Vector2 Offset;
        }

        [System.Serializable]
        public struct BorderEffectTheme
        {
            public ImageTheme Background;
            public RectOffset Offset;
        }

        [System.Serializable]
        public struct ImageTheme
        {
            public Sprite Sprite;
            public Color Color;
        }
    }
}