using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace HBP.UI.Theme
{
    [ExecuteInEditMode]
    public class ThemeElement : MonoBehaviour
    {
        #region Properties
        public enum ZoneEnum { General, Menu, Window, Toolbar, Visualization }
        public enum GeneralEnum { Tooltip }
        public enum MenuEnum { Background, Button, Text, Dropdown, Toggle, SubMenuBackground }
        public enum WindowEnum { Header, Content }
        public enum HeaderEnum { Background, Text, Button }
        public enum ContentEnum { Background, Text, Title, Toggle, Dropdown, Inputfield, ScrollRect, MainButton, SecondaryButton, Item, FileSelector }
        public enum ItemEnum { Background, Text, Toggle, Button } 
        public enum ToolbarEnum { Background, Text, ButtonImage, Toggle, Inputfield, Slider, DropdownText, DropdownImage, ButtonText, ScrollRect, MainEvent, SecondaryEvent, SecondaryText, DropdownTextWithIcon }
        public enum VisualizationEnum { Background, SwapBackground, TransparentBackground, Text, SiteText, MarsAtlasText, BroadmanText, Button, Toggle, Inputfield, Slider, Dropdown, InvisibleButton }
        public enum EffectEnum { Children, RecursiveChildren, Custom, Self}

        public bool IgnoreTheme;
        public ZoneEnum Zone;
        public GeneralEnum General;
        public MenuEnum Menu;
        public WindowEnum Window;
        public HeaderEnum Header;
        public ContentEnum Content;
        public ItemEnum Item;
        public ToolbarEnum Toolbar;
        public VisualizationEnum Visualization;
        public EffectEnum Effect;
        public Graphic[] Graphics;
        Selectable m_Selectable;
        bool m_LastState;
        Theme m_Theme;
        #endregion

        #region Public Methods
        public void Set(Theme theme)
        {
            m_Theme = theme;
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
        void Awake()
        {
            m_Selectable = GetComponent<Selectable>();
            if(m_Selectable) m_LastState = m_Selectable.interactable;
        }
        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Set(ApplicationState.Theme);
            }
        }
        void Update()
        {
            if(m_Selectable)
            {
                if(m_Selectable.interactable != m_LastState)
                {
                    m_LastState = m_Selectable.interactable;
                    Set(m_Theme);
                }
            }
        }

        void SetGeneral(Theme theme)
        {
            switch (General)
            {
                case GeneralEnum.Tooltip:
                    SetImage(GetComponent<Image>(), theme.General.TooltipBackground);
                    SetText(GetComponentInChildren<Text>(), theme.General.TooltipText);
                    break;
            }
        }
        void SetMenu(Theme theme)
        {
            switch (Menu)
            {
                case MenuEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Menu.Background);
                    break;
                case MenuEnum.SubMenuBackground:
                    SetImage(GetComponent<Image>(), theme.Menu.SubMenuBackground);
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
                case HeaderEnum.Button:
                    SetButton(GetComponent<Button>(), theme.Window.Header.Button);
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
                    SetInputField(GetComponent<InputField>(), theme.Window.Content.InputField);
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
                case ContentEnum.Item:
                    SetItem(theme.Window.Content.Item);
                    break;
                case ContentEnum.FileSelector:
                    SetFileSelector(GetComponent<InputField>(), theme.Window.Content.FileSelector);
                    break;
            }
        }
        void SetItem(Theme.ItemTheme theme)
        {
            switch (Item)
            {
                case ItemEnum.Toggle:
                    SetToggle(GetComponent<Toggle>(), theme.Toggle);
                    break;
                case ItemEnum.Button:
                    SetButton(GetComponent<Button>(), theme.Button);
                    break;
                default:
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
                case ToolbarEnum.ScrollRect:
                    SetScrollRect(GetComponent<ScrollRect>(), theme.Toolbar.ScrollRect);
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
                case ToolbarEnum.DropdownTextWithIcon:
                    SetDropdown(GetComponent<Dropdown>(), theme.Toolbar.DropdownTextWithIcon);
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
                    SetInputField(GetComponent<InputField>(), theme.Visualization.InputField);
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
            }
        }
        void SetButton(Button button, Theme.ButtonTheme theme, bool setByAnother = false)
        {
            if (button)
            {
                button.colors = theme.ColorBlock;
                if (!setByAnother)
                {
                    Text[] texts; Image[] icons;
                    FindContents(out texts, out icons);
                    foreach (Text text in texts)
                    {
                        if (button.interactable) SetText(text, theme.Text);
                        else SetText(text, theme.DisabledText);
                    }
                    foreach (Image icon in icons)
                    {
                        if (button.interactable) SetImage(icon, theme.Icon);
                        else SetImage(icon, theme.DisabledIcon);
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
        void SetDropdown(Dropdown dropdown, Theme.DropdownTheme theme, bool setByAnother = false)
        {
            if (dropdown)
            {
                dropdown.colors = theme.ColorBlock;

                if (!setByAnother)
                {
                    Text[] texts; Image[] icons;
                    FindContents(out texts, out icons);
                    foreach (Text text in texts)
                    {
                        if (dropdown.interactable) SetText(text, theme.Text);
                        else SetText(text, theme.DisabledText);
                    }
                    foreach (Image icon in icons)
                    {
                        if (dropdown.interactable) SetImage(icon, theme.Icon);
                        else SetImage(icon, theme.DisabledIcon);
                    }
                }

                // Text.
                if (dropdown.interactable) SetText(dropdown.captionText, theme.Text);
                else SetText(dropdown.captionText, theme.DisabledText);

                // Icon.
                if (dropdown.interactable) SetImage(dropdown.captionImage, theme.Icon);
                else SetImage(dropdown.captionImage, theme.DisabledIcon);

                // Arrow.
                Transform arrow = dropdown.transform.Find("Arrow");
                if (arrow)
                {
                    if (dropdown.interactable) SetImage(arrow.GetComponent<Image>(), theme.Arrow);
                    else SetImage(arrow.GetComponent<Image>(), theme.DisabledArrow);
                }

                if (dropdown.template)
                {
                    SetScrollRect(dropdown.template.GetComponent<ScrollRect>(), theme.Template);
                    Toggle item = dropdown.template.Find("Viewport").Find("Content").Find("Item").GetComponent<Toggle>();
                    SetToggle(item, theme.Item, true);
                }
            }
        }
        void SetToggle(Toggle toggle, Theme.ToggleTheme theme, bool setByAnother = false)
        {
            if (toggle)
            {
                toggle.colors = theme.ColorBlock;
                if(toggle.graphic) toggle.graphic.color = theme.Checkmark;

                if (!setByAnother)
                {
                    Text[] texts; Image[] icons;
                    FindContents(out texts, out icons);
                    foreach (Text text in texts)
                    {
                        if (toggle.interactable) SetText(text, theme.Text);
                        else SetText(text, theme.DisabledText);
                    }
                    foreach (Image icon in icons)
                    {
                        if (toggle.interactable) SetImage(icon, theme.Icon);
                        else SetImage(icon, theme.DisabledIcon);
                    }
                }
                toggle.colors = theme.ColorBlock;
                SetImage((Image)toggle.graphic, theme.Checkmark);
            }
        }
        void SetInputField(InputField inputField, Theme.InputFieldTheme theme, bool setByAnother = false)
        {
            if (inputField)
            {
                if (!setByAnother)
                {
                    Text[] texts; Image[] icons;
                    FindContents(out texts, out icons);
                    foreach (Text t in texts)
                    {
                        if (inputField.interactable) SetText(t, theme.Text);
                        else SetText(t, theme.DisabledText);
                    }
                    foreach (Image icon in icons)
                    {
                        if (inputField.interactable) SetImage(icon, theme.Icon);
                        else SetImage(icon, theme.DisabledIcon);
                    }
                }
                inputField.colors = theme.ColorBlock;
            }
        }
        void SetScrollRect(ScrollRect scrollRect, Theme.ScrollRectTheme theme)
        {
            if(scrollRect)
            {
                SetImage(scrollRect.viewport.GetComponent<Image>(), theme.Background);
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
        void SetSlider(Slider slider,Theme.SliderTheme theme, bool setByAnother = false)
        {
            if (slider)
            {
                slider.colors = theme.ColorBlock;

                if (!setByAnother)
                {
                    Text[] texts; Image[] icons;
                    FindContents(out texts, out icons);
                    foreach (Text text in texts)
                    {
                        if (slider.interactable) SetText(text, theme.Text);
                        else SetText(text, theme.DisabledText);
                    }
                    foreach (Image icon in icons)
                    {
                        if (slider.interactable) SetImage(icon, theme.Icon);
                        else SetImage(icon, theme.DisabledIcon);
                    }
                }

                if (slider.interactable)
                {
                    SetImage(slider.transform.Find("Background").GetComponent<Image>(), theme.Background);
                    if (slider.fillRect) SetImage(slider.fillRect.GetComponent<Image>(), theme.Fill);
                }
                else
                {
                    SetImage(slider.transform.Find("Background").GetComponent<Image>(), theme.DisabledBackground);
                    if (slider.fillRect) SetImage(slider.fillRect.GetComponent<Image>(), theme.DisabledFill);
                }
            }
        }
        void SetFileSelector(InputField fileSelector, Theme.FileSelectorTheme theme, bool setByAnother = false)
        {
            if (fileSelector)
            {
                SetInputField(fileSelector, theme.InputField,true);
                SetButton(fileSelector.GetComponentInChildren<Button>(),theme.Button,true);
            }
        }
        void FindContents(out Text[] texts,out Image[] icons)
        {
            texts = new Text[0]; icons = new Image[0];
            switch (Effect)
            {
                case EffectEnum.Children:
                    List<Text> textList = new List<Text>();
                    List<Image> iconList = new List<Image>();
                    foreach (Transform child in transform)
                    {
                        Text text = child.GetComponent<Text>();
                        if (text) textList.Add(text);
                        Image icon = child.GetComponent<Image>();
                        if (icon) iconList.Add(icon);
                    }
                    texts = textList.ToArray();
                    icons = iconList.ToArray();
                    break;
                case EffectEnum.RecursiveChildren:
                    texts = GetComponentsInChildren<Text>(true);
                    icons = GetComponentsInChildren<Image>(true);
                    break;
                case EffectEnum.Custom:
                    texts = (from graphic in Graphics where graphic is Text select graphic as Text).ToArray();
                    icons = (from graphic in Graphics where graphic is Image select graphic as Image).ToArray();
                    break;
            }
        }
        #endregion
        #endregion
    }
}