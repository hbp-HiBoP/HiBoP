using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Theme
{
    [ExecuteInEditMode]
    public class WindowThemeGestion : MonoBehaviour
    {
        void Start()
        {
            if (Application.isPlaying)
            {
                Set(ApplicationState.Theme);
            }
            else
            {
                Set(new Theme());
            }
        }
        public void Set(Theme theme)
        {
            ThemeElement[] themeElements = GetComponentsInChildren<ThemeElement>(true);
            SetBackgrounds(themeElements,theme);
            SetContentInputfields(themeElements,theme);
            SetContentButtons(themeElements, theme);
            SetTexts(themeElements,theme);
            SetToggles(themeElements, theme);
            SetScrollbars(themeElements,theme);
            SetDropdowns(themeElements, theme);
        }

        void SetBackgrounds(ThemeElement[] themeElements, Theme theme)
        {
            /// Window backgrounds.
            IEnumerable<Image> windowBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowBackground select element.GetComponent<Image>();
            foreach (Image image in windowBackgrounds) SetImage(image, theme.Color.WindowBackground);

            // Header backgrounds.
            IEnumerable<Image> windowHeaderBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowHeaderBackground select element.GetComponent<Image>();
            foreach (Image image in windowHeaderBackgrounds) SetImage(image, theme.Color.WindowHeaderBackground);

            // Content Title backgrounds.
            IEnumerable<Image> windowContentTitleBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowTitleBackground select element.GetComponent<Image>();
            foreach (Image image in windowContentTitleBackgrounds) SetImage(image, theme.Color.WindowContentTitleBackground);

            // Set List background.
            IEnumerable<Image> windowListBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowListBackground select element.GetComponent<Image>();
            foreach (Image image in windowListBackgrounds) SetImage(image, theme.Color.ListBackground);
        }
        void SetContentInputfields(ThemeElement[] themeElements, Theme theme)
        {
            /// Window InputFields.
            IEnumerable<InputField> windowInputFields = from element in themeElements where element.Type == ThemeElement.ElementType.WindowInputField select element.GetComponent<InputField>();
            foreach (InputField inputField in windowInputFields) SetInputField(inputField, theme.Color.InputField);

            // Window FolderSelector.
            IEnumerable<InputField> windowFolderSelectors = from element in themeElements where element.Type == ThemeElement.ElementType.WindowFolderSelector select element.GetComponent<InputField>();
            foreach (InputField inputField in windowFolderSelectors)
            {
                SetInputField(inputField, theme.Color.InputField);
                SetButton(inputField.GetComponentInChildren<Button>(true), theme.Color.FolderSelectorButton);
            }
        }
        void SetContentButtons(ThemeElement[] themeElements, Theme theme)
        {
            /// General Buttons.
            IEnumerable<Button> generalButtons = from element in themeElements where element.Type == ThemeElement.ElementType.WindowGeneralButton select element.GetComponent<Button>();
            foreach (Button button in generalButtons)
            {
                SetButton(button, theme.Color.GeneralButton);
                SetText(button.GetComponentInChildren<Text>(), theme.Color.ContentGeneralButtonLabel, theme.Font.WindowContentGeneralButton);
            }

            // Content Buttons.
            IEnumerable<Button> otherButtons = from element in themeElements where element.Type == ThemeElement.ElementType.WindowOtherButton select element.GetComponent<Button>();
            foreach (Button button in otherButtons)
            {
                SetButton(button, theme.Color.OtherButton);
                SetText(button.GetComponentInChildren<Text>(), theme.Color.ContentOtherButtonLabel, theme.Font.WindowContentOtherButton);
            }

            // Content Buttons.
            IEnumerable<Button> menuButtons = from element in themeElements where element.Type == ThemeElement.ElementType.MenuButton select element.GetComponent<Button>();
            foreach (Button button in menuButtons)
            {
                SetButton(button, theme.Color.MenuButton);
                SetText(button.GetComponentInChildren<Text>(), theme.Color.ContentOtherButtonLabel, theme.Font.WindowContentOtherButton);
            }
        }
        void SetTexts(ThemeElement[] themeElements, Theme theme)
        {
            // Set Header title.
            IEnumerable<Text> headerTitles = from element in themeElements where element.Type == ThemeElement.ElementType.WindowHeaderTitle select element.GetComponent<Text>();
            foreach (Text headerTitle in headerTitles) SetText(headerTitle, theme.Color.HeaderTitleLabel,theme.Font.WindowHeader);

            // Set content titles.
            IEnumerable<Text> contentTitles = from element in themeElements where element.Type == ThemeElement.ElementType.WindowTitle select element.GetComponent<Text>();
            foreach (Text text in contentTitles) SetText(text, theme.Color.ContentTitleLabel, theme.Font.WindowContentTitle);

            //Set content labels.
            IEnumerable<Text> contentLabels = from element in themeElements where element.Type == ThemeElement.ElementType.WindowLabel select element.GetComponent<Text>();
            foreach (Text text in contentLabels) SetText(text, theme.Color.ContentNormalLabel, theme.Font.WindowContentLabel);
        }
        void SetToggles(ThemeElement[] themeElements, Theme theme)
        {
            // Set Toggles.
            IEnumerable<Toggle> windowToggles = from element in themeElements where element.Type == ThemeElement.ElementType.WindowToggle select element.GetComponent<Toggle>();
            foreach (Toggle windowToggle in windowToggles) SetToggle(windowToggle, theme.Color.WindowToggle);

            IEnumerable<Toggle> menuToggles = from element in themeElements where element.Type == ThemeElement.ElementType.MenuToggle select element.GetComponent<Toggle>();
            foreach (Toggle menuToggle in menuToggles) SetToggle(menuToggle, theme.Color.MenuToggle);
        }
        void SetScrollbars(ThemeElement[] themeElements, Theme theme)
        {
            // Set Scrollbars.
            IEnumerable<Scrollbar> ScrollBars = from element in themeElements where element.Type == ThemeElement.ElementType.WindowScrollbar select element.GetComponent<Scrollbar>();
            foreach (Scrollbar scrollbar in ScrollBars) SetScrollbar(scrollbar, theme.Color.ScrollbarBackground, theme.Color.Scrollbar);
        }
        void SetDropdowns(ThemeElement[] themeElements, Theme theme)
        {
            // Set Dropdowns.
            IEnumerable<Dropdown> windowDropdowns = from element in themeElements where element.Type == ThemeElement.ElementType.WindowDropdown select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in windowDropdowns) SetDropdown(dropdown, theme.Color.WindowDropdown);

            IEnumerable<Dropdown> menuDropdowns = from element in themeElements where element.Type == ThemeElement.ElementType.MenuDropdown select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in menuDropdowns) SetDropdown(dropdown, theme.Color.MenuDropdown);
        }

        void SetButton(Button button, ColorBlock colorBlock)
        {
            if(button)
            {
                button.colors = colorBlock;
            }
        }
        void SetToggle(Toggle toggle, ColorBlock colorBlock)
        {
           if(toggle) toggle.colors = colorBlock;
        }
        void SetImage(Image image,Color color)
        {
            if(image)
            {
                image.color = color;
                image.enabled = false; image.enabled = true;
            }
        }
        void SetInputField(InputField inputField,ColorBlock colorBlock)
        {
            if(inputField) inputField.colors = colorBlock;
        }
        void SetText(Text text,Color color,FontData fontData)
        {
            if(text)
            {
                text.color = color;
                text.alignByGeometry = fontData.alignByGeometry;
                text.alignment = fontData.alignment;
                text.resizeTextForBestFit = fontData.bestFit;
                text.font = fontData.font;
                text.fontSize = fontData.fontSize;
                text.fontStyle = fontData.fontStyle;
                text.horizontalOverflow = fontData.horizontalOverflow;
                text.lineSpacing = fontData.lineSpacing;
                text.resizeTextMaxSize = fontData.maxSize;
                text.resizeTextMinSize = fontData.minSize;
                text.supportRichText = fontData.richText;
                text.verticalOverflow = fontData.verticalOverflow;
            }
        }
        void SetScrollbar(Scrollbar scrollbar,Color backgroundColor, ColorBlock colorBlock)
        {
            if(scrollbar)
            {
                scrollbar.colors = colorBlock;
                scrollbar.GetComponent<Image>().color = backgroundColor;
            }
        }
        void SetDropdown(Dropdown dropdown, ColorBlock colorBlock)
        {
            if(dropdown) dropdown.colors = colorBlock;
        }
    }
}

