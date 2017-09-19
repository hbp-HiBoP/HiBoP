using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Theme
{
    public class ThemeElement : MonoBehaviour
    {
        #region Properties
        public enum ZoneEnum { General, Menu, Window, Toolbar, Visualization }
        public enum MenuEnum { Background, Button, Text, Dropdown, Toggle }
        public enum WindowEnum { Header, Content }
        public enum HeaderEnum { Background, Text }
        public enum ContentEnum { Background, Text, Title, Toggle, Dropdown, Inputfield, ScrollRect, MainButton, SecondaryButton }
        public enum ToolbarEnum { Background, Text, ButtonImage, Toggle, Inputfield, Slider, DropdownText, DropdownImage, ButtonText, TimelineText, MainEvent, SecondaryEvent, SecondaryText }
        public enum VisualizationEnum { Background, SwapBackground, TransparentBackground, Text, SiteText, MarsAtlasText, BroadmanText, Button, Toggle, Inputfield, Slider, Dropdown, InvisibleButton }

        public bool IgnoreTheme;
        public ZoneEnum Zone;
        public MenuEnum Menu;
        public WindowEnum Window;
        public HeaderEnum Header;
        public ContentEnum Content;
        public ToolbarEnum Toolbar;
        public VisualizationEnum Visualization;
        #endregion

        #region Public Methods
        public void Set(Theme theme)
        {
            if(!IgnoreTheme)
            {
                switch (Zone)
                {
                    case ZoneEnum.General: SetGeneral(theme); break;
                    case ZoneEnum.Menu: SetMenu(theme); break;
                    case ZoneEnum.Window: SetWindow(theme); break;
                    case ZoneEnum.Toolbar: SetToolbar(theme); break;
                    case ZoneEnum.Visualization: SetVisualization(theme); break;
                }
            }
        }
        #endregion

        #region Private Methods
        void SetGeneral(Theme theme) { }
        void SetMenu(Theme theme)
        {
            switch (Menu)
            {
                case MenuEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Menu.Background);
                    break;
                case MenuEnum.Button:
                    SetButton(GetComponent<Button>(), theme.Menu.Button);
                    break;
                case MenuEnum.Text:
                    SetText(GetComponent<Text>(), theme.Menu.Text);
                    break;
                case MenuEnum.Dropdown:
                    SetDropdown(GetComponent<Dropdown>(), theme.Menu.Dropdown);
                    break;
                case MenuEnum.Toggle:
                    SetToggle(GetComponent<Toggle>(), theme.Menu.Toggle);
                    break;
            }
        }
        void SetWindow(Theme theme)
        {
            switch (Window)
            {
                case WindowEnum.Header: SetHeader(theme); break;
                case WindowEnum.Content: SetContent(theme); break;
            }
        }
        void SetHeader(Theme theme)
        {
            switch (Header)
            {
                case HeaderEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Window.Header.Background);
                    break;
                case HeaderEnum.Text:
                    SetText(GetComponent<Text>(), theme.Window.Header.Text);
                    break;
            }
        }
        void SetContent(Theme theme)
        {
            switch (Content)
            {
                case ContentEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Window.Content.Background);
                    break;
                case ContentEnum.Text:
                    SetText(GetComponent<Text>(), theme.Window.Content.Text);
                    break;
                case ContentEnum.Title:
                    SetTitle(GetComponent<Image>(), theme.Window.Content.Title);
                    break;
                case ContentEnum.Toggle:
                    SetToggle(GetComponent<Toggle>(), theme.Window.Content.Toggle);
                    break;
                case ContentEnum.Dropdown:
                    SetDropdown(GetComponent<Dropdown>(), theme.Window.Content.Dropdown);
                    break;
                case ContentEnum.Inputfield:
                    SetInputField(GetComponent<InputField>(), theme.Window.Content.Inputfield);
                    break;
                case ContentEnum.ScrollRect:
                    SetScrollRect(GetComponent<ScrollRect>(), theme.Window.Content.ScrollRect);
                    break;
                case ContentEnum.MainButton:
                    SetButton(GetComponent<Button>(), theme.Window.Content.MainButton);
                    break;
                case ContentEnum.SecondaryButton:
                    SetButton(GetComponent<Button>(), theme.Window.Content.SecondaryButton);
                    break;
            }
        }
        void SetToolbar(Theme theme)
        {
            switch (Toolbar)
            {
                case ToolbarEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Toolbar.Background);
                    break;
                case ToolbarEnum.Text:
                    SetText(GetComponent<Text>(), theme.Toolbar.Text);
                    break;
                case ToolbarEnum.TimelineText:
                    SetText(GetComponent<Text>(), theme.Toolbar.TimelineText);
                    break;
                case ToolbarEnum.SecondaryText:
                    SetText(GetComponent<Text>(), theme.Toolbar.SecondaryText);
                    break;
                case ToolbarEnum.ButtonImage:
                    SetButton(GetComponent<Button>(), theme.Toolbar.ButtonImage);
                    break;
                case ToolbarEnum.ButtonText:
                    SetButton(GetComponent<Button>(), theme.Toolbar.ButtonText);
                    break;
                case ToolbarEnum.Toggle:
                    SetToggle(GetComponent<Toggle>(), theme.Toolbar.Toggle);
                    break;
                case ToolbarEnum.Inputfield:
                    SetInputField(GetComponent<InputField>(), theme.Toolbar.InputField);
                    break;
                case ToolbarEnum.Slider:
                    SetSlider(GetComponent<Slider>(), theme.Toolbar.Slider);
                    break;
                case ToolbarEnum.DropdownText:
                    SetDropdown(GetComponent<Dropdown>(), theme.Toolbar.DropdownText);
                    break;
                case ToolbarEnum.DropdownImage:
                    SetDropdown(GetComponent<Dropdown>(), theme.Toolbar.DropdownImage);
                    break;
                case ToolbarEnum.MainEvent:
                    SetImage(GetComponent<Image>(), theme.Toolbar.MainEvent);
                    break;
                case ToolbarEnum.SecondaryEvent:
                    SetImage(GetComponent<Image>(), theme.Toolbar.SecondaryEvent);
                    break;
            }
        }
        void SetVisualization(Theme theme)
        {
            switch (Visualization)
            {
                case VisualizationEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Visualization.Background);
                    break;
                case VisualizationEnum.SwapBackground:
                    SetImage(GetComponent<Image>(), theme.Visualization.SwapBackground);
                    break;
                case VisualizationEnum.TransparentBackground:
                    SetImage(GetComponent<Image>(), theme.Visualization.TransparentBackground);
                    break;
                case VisualizationEnum.Text:
                    SetText(GetComponent<Text>(), theme.Visualization.Text);
                    break;
                case VisualizationEnum.SiteText:
                    SetText(GetComponent<Text>(), theme.Visualization.SiteText);
                    break;
                case VisualizationEnum.MarsAtlasText:
                    SetText(GetComponent<Text>(), theme.Visualization.MarsAtlasText);
                    break;
                case VisualizationEnum.BroadmanText:
                    SetText(GetComponent<Text>(), theme.Visualization.BroadmanText);
                    break;
                case VisualizationEnum.Button:
                    SetButton(GetComponent<Button>(), theme.Visualization.Button);
                    break;
                case VisualizationEnum.InvisibleButton:
                    SetButton(GetComponent<Button>(), theme.Visualization.InvisibleButton);
                    break;
                case VisualizationEnum.Toggle:
                    SetToggle(GetComponent<Toggle>(), theme.Visualization.Toggle);
                    break;
                case VisualizationEnum.Inputfield:
                    SetInputField(GetComponent<InputField>(), theme.Visualization.Inputfield);
                    break;
                case VisualizationEnum.Slider:
                    SetSlider(GetComponent<Slider>(), theme.Visualization.Slider);
                    break;
                case VisualizationEnum.Dropdown:
                    SetDropdown(GetComponent<Dropdown>(), theme.Visualization.Dropdown);
                    break;
            }
        }

        #region Tools
        void SetImage(Image image, Color color)
        {
            if (image)
            {
                image.color = color;
                image.SetAllDirty();
            }
        }
        void SetButton(Button button, Theme.ButtonTheme theme)
        {
            if (button)
            {
                button.colors = theme.ColorBlock;
                foreach (Transform child in button.transform)
                {
                    Text buttonText = child.GetComponent<Text>();
                    if (buttonText)
                    {
                        SetText(buttonText, theme.Text);
                    }
                }
            }
        }
        void SetText(Text text, Theme.TextTheme theme)
        {
            if (text)
            {
                text.color = theme.Color;
                text.alignByGeometry = theme.Font.alignByGeometry;
                text.resizeTextForBestFit = theme.Font.bestFit;
                text.font = theme.Font.font;
                text.fontSize = theme.Font.fontSize;
                text.fontStyle = theme.Font.fontStyle;
                text.horizontalOverflow = theme.Font.horizontalOverflow;
                text.lineSpacing = theme.Font.lineSpacing;
                text.resizeTextMaxSize = theme.Font.maxSize;
                text.resizeTextMinSize = theme.Font.minSize;
                text.supportRichText = theme.Font.richText;
                text.verticalOverflow = theme.Font.verticalOverflow;
            }
        }
        void SetDropdown(Dropdown dropdown, Theme.DropdownTheme theme)
        {
            if (dropdown)
            {
                dropdown.colors = theme.ColorBlock;
                SetText(dropdown.captionText, theme.Text);
                Transform arrow = dropdown.transform.Find("Arrow");
                if (arrow) SetImage(arrow.GetComponent<Image>(), theme.ArrowColor);

                if (dropdown.template)
                {
                    SetScrollRect(dropdown.template.GetComponent<ScrollRect>(), theme.Template);
                    Toggle item = dropdown.template.Find("Viewport").Find("Content").Find("Item").GetComponent<Toggle>();
                    SetToggle(item, theme.Item);
                }
            }
        }
        void SetToggle(Toggle toggle, Theme.ToggleTheme theme)
        {
            if (toggle)
            {
                toggle.colors = theme.ColorBlock;
                if(toggle.graphic) toggle.graphic.color = theme.Checkmark;
                foreach (Transform child in toggle.transform)
                {
                    Text toggleText = child.GetComponent<Text>();
                    if (toggleText)
                    {
                        SetText(toggleText, theme.Text);
                    }
                }
            }
        }
        void SetInputField(InputField inputField, Theme.InputFieldTheme theme)
        {
            if (inputField)
            {
                inputField.colors = theme.ColorBlock;
                SetText(inputField.transform.Find("Text").GetComponent<Text>(),theme.Text);
            }
        }
        void SetScrollRect(ScrollRect scrollRect, Theme.ScrollRectTheme theme )
        {
            if(scrollRect)
            {
                SetImage(scrollRect.GetComponent<Image>(), theme.Background);
                SetScrollbar(scrollRect.verticalScrollbar,theme.Scrollbar);
                SetScrollbar(scrollRect.horizontalScrollbar, theme.Scrollbar);
            }
        }
        void SetScrollbar(Scrollbar scrollbar, Theme.ScrollBarTheme theme)
        {
            if (scrollbar)
            {
                scrollbar.colors = theme.ColorBlock;
                SetImage(scrollbar.GetComponent<Image>(), theme.Background);
                SetImage(scrollbar.handleRect.GetComponent<Image>(), theme.Handle);
            }
        }
        void SetTitle(Image title, Theme.TitleTheme theme)
        {
            if(title)
            {
                SetImage(title, theme.Background);
                foreach(Text text in title.GetComponentsInChildren<Text>())
                {
                    SetText(text, theme.Text);
                }
            }
        }
        void SetSlider(Slider slider,Theme.SliderTheme theme)
        {
            if (slider)
            {
                SetImage(slider.transform.Find("Background").GetComponent<Image>(), theme.Background);
                SetImage(slider.fillRect.GetComponent<Image>(), theme.Fill);
                SetImage(slider.handleRect.GetComponent<Image>(), theme.Handle);
            }
        }
        #endregion
        #endregion
    }
}