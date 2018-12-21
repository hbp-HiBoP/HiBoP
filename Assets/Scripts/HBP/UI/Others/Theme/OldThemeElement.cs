using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace HBP.UI.Theme
{
    [ExecuteInEditMode]
    public class OldThemeElement : MonoBehaviour
    {
        #region Properties
        public enum ZoneEnum { General, Menu, Window, Toolbar, Visualization }
        public enum GeneralEnum { Tooltip, Background, LoadingCircle, DialogBox }
        public enum MenuEnum { Background, Button, Text, Dropdown, Toggle, SubMenuBackground }
        public enum WindowEnum { Header, Content, Selector, Shadow }
        public enum HeaderEnum { Background, Text, CloseButton, MaximizeButton, MinimizeButton }
        public enum ContentEnum { Background, Text, Title, Toggle, Dropdown, Inputfield, ScrollRect, MainButton, SecondaryButton, Item, FileSelector }
        public enum ItemEnum { Background, Text, Toggle, Button, ContainerBloc, MainBloc, SecondaryBloc, Scrollbar } 
        public enum ToolbarEnum { Background, Text, ButtonImage, Toggle, Inputfield, Slider, DropdownText, DropdownImage, ButtonText, ScrollRect, MainEvent, SecondaryEvent, SecondaryText, DropdownTextWithIcon }
        public enum VisualizationEnum { Background, SwapBackground, TransparentBackground, Text, SiteText, MarsAtlasText, BroadmanText, Button, Toggle, Inputfield, Slider, Dropdown, InvisibleButton, Border }
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
            // Prevents errors
            return;
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
                Set(ApplicationState.UserPreferences.Theme);
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
                case GeneralEnum.Background:
                    SetImage(GetComponent<Image>(), theme.General.Background.Color,theme.General.Background.Sprite);
                    break;
                case GeneralEnum.LoadingCircle:
                    SetLoadingCircle(theme.General.LoadingCircle, transform);
                    break;
                case GeneralEnum.DialogBox:
                    SetDialogBox(theme.General.DialogBox, transform);
                    break;
            }
        }
        void SetMenu(Theme theme)
        {
            switch (Menu)
            {
                case MenuEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Menu.Background.Color,theme.Menu.Background.Sprite);
                    break;
                case MenuEnum.SubMenuBackground:
                    SetImage(GetComponent<Image>(), theme.Menu.SubMenuBackground.Color, theme.Menu.SubMenuBackground.Sprite);
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
                case WindowEnum.Selector: SetBorderEffect(theme.Window.Selector); break;
                case WindowEnum.Shadow: SetBorderEffect(theme.Window.Shadow); break;
            }
        }
        void SetHeader(Theme theme)
        {
            switch (Header)
            {
                case HeaderEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Window.Header.Background.Color, theme.Window.Header.Background.Sprite);
                    break;
                case HeaderEnum.Text:
                    SetText(GetComponent<Text>(), theme.Window.Header.Text);
                    break;
                case HeaderEnum.CloseButton:
                    SetButton(GetComponent<Button>(), theme.Window.Header.CloseButton);
                    break;
            }
        }
        void SetContent(Theme theme)
        {
            switch (Content)
            {
                case ContentEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Window.Content.Background.Color, theme.Window.Content.Background.Sprite);
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
                case ItemEnum.Text:
                    SetText(GetComponent<Text>(), theme.Text);
                    break;
                case ItemEnum.Background:
                    SetImage(GetComponent<Image>(), theme.Background);
                    break;
                case ItemEnum.Toggle:
                    SetToggle(GetComponent<Toggle>(), theme.Toggle);
                    break;
                case ItemEnum.Button:
                    SetButton(GetComponent<Button>(), theme.Button);
                    break;
                case ItemEnum.ContainerBloc:
                    SetButton(GetComponent<Button>(), theme.Bloc.Container);
                    break;
                case ItemEnum.MainBloc:
                    SetButton(GetComponent<Button>(), theme.Bloc.MainBloc);
                    break;
                case ItemEnum.SecondaryBloc:
                    SetButton(GetComponent<Button>(), theme.Bloc.SecondaryBloc);
                    break;
                case ItemEnum.Scrollbar:
                    SetScrollbar(GetComponent<Scrollbar>(), theme.Scrollbar);
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
                case VisualizationEnum.Border:
                    SetImage(GetComponent<Image>(), theme.Visualization.Border);
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
        void SetImage(Image image, Color color, Sprite sprite)
        {
            if(image)
            {
                image.color = color;
                image.sprite = sprite;
            }
        }
        void SetButton(Button button, Theme.ButtonTheme theme, bool setByAnother = false)
        {
            if (button)
            {
                Image image = button.targetGraphic as Image;
                if (image)
                {
                    image.color = theme.Background.Color;
                    image.sprite = theme.Background.Sprite;
                }
                button.colors = theme.ColorBlock.Colors;
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
                dropdown.colors = theme.ColorBlock.Colors;
                Image image = dropdown.targetGraphic as Image;
                if(image)
                {
                    image.color = theme.Background.Color;
                    image.sprite = theme.Background.Sprite;
                }
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
                    SetDropdownItem(item, theme.Item);
                }
            }
        }
        void SetDropdownItem(Toggle toggle, Theme.ToggleTheme theme)
        {
            if (toggle)
            {
                toggle.colors = theme.ColorBlock.Colors;
                if (toggle.graphic) toggle.graphic.color = theme.Checkmark;
                Text[] texts; Image[] icons;
                FindContents(out texts, out icons, toggle.transform, true);
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
                toggle.colors = theme.ColorBlock.Colors;
                SetImage((Image)toggle.graphic, theme.Checkmark);
            }
        }
        void SetToggle(Toggle toggle, Theme.ToggleTheme theme, bool setByAnother = false)
        {
            if (toggle)
            {
                toggle.colors = theme.ColorBlock.Colors;
                Image image = toggle.targetGraphic as Image;
                if (image)
                {
                    image.color = theme.Background.Color;
                    image.sprite = theme.Background.Sprite;
                }
                if (toggle.graphic) toggle.graphic.color = theme.Checkmark;

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
                toggle.colors = theme.ColorBlock.Colors;
                SetImage((Image)toggle.graphic, theme.Checkmark);
            }
        }
        void SetInputField(InputField inputField, Theme.InputFieldTheme theme, bool setByAnother = false)
        {
            if (inputField)
            {
                Image image = inputField.targetGraphic as Image;
                if (image)
                {
                    image.color = theme.Background.Color;
                    image.sprite = theme.Background.Sprite;
                }
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
                inputField.colors = theme.ColorBlock.Colors;
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
                scrollbar.colors = theme.ColorBlock.Colors;
                SetImage(scrollbar.GetComponent<Image>(), theme.Background);
                SetImage(scrollbar.handleRect.GetComponent<Image>(), theme.Handle);
            }
        }
        void SetTitle(Image title, Theme.TitleTheme theme)
        {
            if(title)
            {
                SetImage(title, theme.Background.Color, theme.Background.Sprite);
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
                slider.colors = theme.ColorBlock.Colors;

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
                SetInputField(fileSelector, theme.InputField, false);
                SetButton(fileSelector.GetComponentInChildren<Button>(),theme.Button,true);
            }
        }
        void SetLoadingCircle(Theme.LoadingCircleTheme theme, Transform loadingCircle)
        {
            SetImage(loadingCircle.GetComponent<Image>(), theme.Circle);
            SetImage(loadingCircle.Find("Fill").GetComponent<Image>(), theme.Fill);
            SetImage(loadingCircle.Find("Fill").Find("BackGround").GetComponent<Image>(), theme.Background);
            SetImage(loadingCircle.Find("Informations").GetComponent<Image>(), theme.Background);
            SetText(loadingCircle.Find("Informations").Find("information").GetComponent<Text>(), theme.Text);
            SetText(loadingCircle.Find("Informations").Find("information").Find("LoadingEffect").GetComponent<Text>(), theme.Text);
        }
        void SetDialogBox(Theme.DialogBoxTheme theme, Transform dialogBox)
        {
            SetImage(dialogBox.Find("Window").GetComponent<Image>(), theme.Background.Color,theme.Background.Sprite);
            SetText(dialogBox.Find("Window").Find("Corps").Find("Text").Find("Title").GetComponent<Text>(), theme.Title);
            SetText(dialogBox.Find("Window").Find("Corps").Find("Text").Find("Message").GetComponent<Text>(), theme.Text);
            foreach (Button button in dialogBox.Find("Window").Find("Buttons").GetComponentsInChildren<Button>())
            {
                SetButton(button, theme.Button);
            }
        }
        void SetBorderEffect(Theme.BorderEffectTheme theme)
        {
            RectTransform rectTransform = transform as RectTransform;
            Image image = GetComponent<Image>();
            if(image != null && rectTransform != null)
            {
                image.sprite = theme.Background.Sprite;
                image.color = theme.Background.Color;
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = new Vector2(theme.Offset.left,theme.Offset.bottom);
                rectTransform.offsetMax = new Vector2(-theme.Offset.right, -theme.Offset.top);
            }
        }
        void FindContents(out Text[] texts,out Image[] icons, Transform parent = null, bool forceRecursiveChildren = false)
        {
            texts = new Text[0]; icons = new Image[0];
            Transform contentsParentTransform = parent;
            if (parent == null) contentsParentTransform = transform;
            if (forceRecursiveChildren)
            {
                texts = contentsParentTransform.GetComponentsInChildren<Text>(true);
                icons = contentsParentTransform.GetComponentsInChildren<Image>(true);
            }
            else
            {
                switch (Effect)
                {
                    case EffectEnum.Children:
                        List<Text> textList = new List<Text>();
                        List<Image> iconList = new List<Image>();
                        foreach (Transform child in contentsParentTransform)
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
                        texts = contentsParentTransform.GetComponentsInChildren<Text>(true);
                        icons = contentsParentTransform.GetComponentsInChildren<Image>(true);
                        break;
                    case EffectEnum.Custom:
                        if (!parent)
                        {
                            texts = (from graphic in Graphics where graphic is Text select graphic as Text).ToArray();
                            icons = (from graphic in Graphics where graphic is Image select graphic as Image).ToArray();
                        }
                        break;
                }
            }
        }
        #endregion
        #endregion
    }
}