using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Theme
{
    [ExecuteInEditMode]
    public class ThemeManager : MonoBehaviour
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
            ThemeElement[] themeElements = GetComponentsInChildren<ThemeElement>(true).Where((element) => !element.IgnoreTheme).ToArray();
            SetBackgrounds(themeElements,theme);
            SetContentInputfields(themeElements,theme);
            SetContentButtons(themeElements, theme);
            SetTexts(themeElements,theme);
            SetToggles(themeElements, theme);
            SetScrollbars(themeElements,theme);
            SetDropdowns(themeElements, theme);
            SetSliders(themeElements, theme);
        }

        void SetBackgrounds(ThemeElement[] themeElements, Theme theme)
        {
            /// Window backgrounds.
            IEnumerable<Image> windowBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowBackground select element.GetComponent<Image>();
            foreach (Image image in windowBackgrounds) SetImage(image, theme.Window.Content.Background);

            // Header backgrounds.
            IEnumerable<Image> windowHeaderBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowHeaderBackground select element.GetComponent<Image>();
            foreach (Image image in windowHeaderBackgrounds) SetImage(image, theme.Window.Header.Background);

            // Content Title backgrounds.
            IEnumerable<Image> windowContentTitleBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowTitleBackground select element.GetComponent<Image>();
            foreach (Image image in windowContentTitleBackgrounds) SetImage(image, theme.Window.Content.Title.Background);

            // Set List background.
            IEnumerable<Image> windowListBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.WindowListBackground select element.GetComponent<Image>();
            foreach (Image image in windowListBackgrounds) SetImage(image, theme.Window.Content.List.Background);

            // Set Toolbar background
            IEnumerable<Image> toolbarBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarBackground select element.GetComponent<Image>();
            foreach (Image image in toolbarBackgrounds) SetImage(image, theme.Toolbar.Background);

            // Set Visualization background
            IEnumerable<Image> visualizationBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationBackground select element.GetComponent<Image>();
            foreach (Image image in visualizationBackgrounds) SetImage(image, theme.Visualization.Background);

            // Set Visualization alternative background
            IEnumerable<Image> visualizationAlternativeBackgrounds = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationAlternativeBackground select element.GetComponent<Image>();
            foreach (Image image in visualizationAlternativeBackgrounds) SetImage(image, theme.Visualization.AlternativeBackground);
        }
        void SetContentInputfields(ThemeElement[] themeElements, Theme theme)
        {
            /// Window InputFields.
            IEnumerable<InputField> windowInputFields = from element in themeElements where element.Type == ThemeElement.ElementType.WindowInputField select element.GetComponent<InputField>();
            foreach (InputField inputField in windowInputFields) SetInputField(inputField, theme.Window.Content.Inputfield.ColorBlock);

            // Toolbar InputFields
            IEnumerable<InputField> toolbarInputFields = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarInputField select element.GetComponent<InputField>();
            foreach (InputField inputField in toolbarInputFields) SetInputField(inputField, theme.Toolbar.InputField.ColorBlock);

            // Visualization InputFields
            IEnumerable<InputField> visualizationInputFields = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationInputField select element.GetComponent<InputField>();
            foreach (InputField inputField in visualizationInputFields) SetInputField(inputField, theme.Visualization.InputField.ColorBlock);
        }
        void SetContentButtons(ThemeElement[] themeElements, Theme theme)
        {
            /// General Buttons.
            IEnumerable<Button> generalButtons = from element in themeElements where element.Type == ThemeElement.ElementType.WindowGeneralButton select element.GetComponent<Button>();
            foreach (Button button in generalButtons)
            {
                SetButton(button, theme.Window.Content.MainButton.ColorBlock);
                SetText(button.GetComponentInChildren<Text>(), theme.Window.Content.MainButton.Text.Color, theme.Window.Content.MainButton.Text.Font);
            }

            // Content Buttons.
            IEnumerable<Button> otherButtons = from element in themeElements where element.Type == ThemeElement.ElementType.WindowOtherButton select element.GetComponent<Button>();
            foreach (Button button in otherButtons)
            {
                SetButton(button, theme.Window.Content.SecondaryButton.ColorBlock);
                SetText(button.GetComponentInChildren<Text>(), theme.Window.Content.SecondaryButton.Text.Color, theme.Window.Content.SecondaryButton.Text.Font);
            }

            // Content Buttons.
            //IEnumerable<Button> menuButtons = from element in themeElements where element.Type == ThemeElement.ElementType.MenuButton select element.GetComponent<Button>();
            //foreach (Button button in menuButtons)
            //{
            //    SetButton(button, theme.Menu.Button.ColorBlock);
            //    foreach (var text in button.GetComponentsInChildren<Text>())
            //    {
            //        SetText(text, theme.Color.ContentOtherButtonLabel, theme.Font.Menu.FontData);
            //    }
            //}

            // Toolbar Buttons.
            IEnumerable<Button> toolbarButtons = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarButton select element.GetComponent<Button>();
            foreach (Button button in toolbarButtons)
            {
                SetButton(button, theme.Toolbar.Button.ColorBlock);
                foreach (var text in button.GetComponentsInChildren<Text>())
                {
                    SetText(text, theme.Toolbar.Button.Text.Color, theme.Toolbar.Button.Text.Font);
                }
            }

            // Visualization Buttons.
            IEnumerable<Button> visualizationButtons = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationButton select element.GetComponent<Button>();
            foreach (Button button in visualizationButtons)
            {
                SetButton(button, theme.Visualization.Button.ColorBlock);
                foreach (var text in button.GetComponentsInChildren<Text>())
                {
                    SetText(text, theme.Visualization.Button.Text.Color, theme.Visualization.Button.Text.Font);
                }
            }
        }
        void SetTexts(ThemeElement[] themeElements, Theme theme)
        {
            // Set Header title.
            IEnumerable<Text> headerTitles = from element in themeElements where element.Type == ThemeElement.ElementType.WindowHeaderTitle select element.GetComponent<Text>();
            foreach (Text headerTitle in headerTitles) SetText(headerTitle, theme.Window.Header.Text.Color, theme.Window.Header.Text.Font);

            // Set content titles.
            IEnumerable<Text> contentTitles = from element in themeElements where element.Type == ThemeElement.ElementType.WindowTitle select element.GetComponent<Text>();
            foreach (Text text in contentTitles) SetText(text, theme.Window.Content.Title.Text.Color, theme.Window.Content.Title.Text.Font);

            //Set content labels.
            IEnumerable<Text> contentLabels = from element in themeElements where element.Type == ThemeElement.ElementType.WindowLabel select element.GetComponent<Text>();
            foreach (Text text in contentLabels) SetText(text, theme.Window.Content.Text.Color, theme.Window.Content.Text.Font);

            //Set visualization labels.
            IEnumerable<Text> visualizationLabels = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationLabel select element.GetComponent<Text>();
            foreach (Text text in visualizationLabels) SetText(text, theme.Visualization.Text.Color, theme.Visualization.Text.Font);
        }
        void SetToggles(ThemeElement[] themeElements, Theme theme)
        {
            // Set Toggles.
            IEnumerable<Toggle> windowToggles = from element in themeElements where element.Type == ThemeElement.ElementType.WindowToggle select element.GetComponent<Toggle>();
            foreach (Toggle windowToggle in windowToggles) SetToggle(windowToggle, theme.Window.Content.Toggle.ColorBlock);

            IEnumerable<Toggle> menuToggles = from element in themeElements where element.Type == ThemeElement.ElementType.MenuToggle select element.GetComponent<Toggle>();
            foreach (Toggle menuToggle in menuToggles) SetToggle(menuToggle, theme.Menu.Toggle.ColorBlock);

            IEnumerable<Toggle> toolbarToggles = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarToggle select element.GetComponent<Toggle>();
            foreach (Toggle toolbarToggle in toolbarToggles) SetToggle(toolbarToggle, theme.Toolbar.Toggle.ColorBlock, theme.Toolbar.Toggle.Checkmark);

            IEnumerable<Toggle> visualizationToggles = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationToggle select element.GetComponent<Toggle>();
            foreach (Toggle toggle in visualizationToggles) SetToggle(toggle, theme.Visualization.Toggle.ColorBlock, theme.Visualization.Toggle.Checkmark);
        }
        void SetScrollbars(ThemeElement[] themeElements, Theme theme)
        {
            // Set Scrollbars.
            IEnumerable<Scrollbar> ScrollBars = from element in themeElements where element.Type == ThemeElement.ElementType.WindowScrollbar select element.GetComponent<Scrollbar>();
            foreach (Scrollbar scrollbar in ScrollBars) SetScrollbar(scrollbar, theme.Window.Content.List.ScrollBar.Background, theme.Window.Content.List.ScrollBar.ColorBlock);
        }
        void SetDropdowns(ThemeElement[] themeElements, Theme theme)
        {
            // Set Dropdowns.
            IEnumerable<Dropdown> windowDropdowns = from element in themeElements where element.Type == ThemeElement.ElementType.WindowDropdown select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in windowDropdowns) SetDropdown(dropdown, theme.Window.Content.Dropdown.ColorBlock);

            IEnumerable<Dropdown> menuDropdowns = from element in themeElements where element.Type == ThemeElement.ElementType.MenuDropdown select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in menuDropdowns) SetDropdown(dropdown, theme.Menu.Dropdown.ColorBlock);

            IEnumerable<Dropdown> toolbarDropdownsText = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarDropdownText select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in toolbarDropdownsText) SetDropdown(dropdown, theme.Toolbar.DropdownText.ColorBlock);

            IEnumerable<Dropdown> toolbarDropdownsImage = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarDropdownImage select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in toolbarDropdownsImage) SetDropdown(dropdown, theme.Toolbar.DropdownImage.ColorBlock);

            IEnumerable<Dropdown> visualizationDropdowns = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationDropdown select element.GetComponent<Dropdown>();
            foreach (Dropdown dropdown in visualizationDropdowns) SetDropdown(dropdown, theme.Visualization.Dropdown.ColorBlock);
        }
        void SetSliders(ThemeElement[] themeElements, Theme theme)
        {
            IEnumerable<Slider> toolbarSliders = from element in themeElements where element.Type == ThemeElement.ElementType.ToolbarSlider select element.GetComponent<Slider>();
            foreach (Slider slider in toolbarSliders) SetSlider(slider, theme.Toolbar.Slider.Background, theme.Toolbar.Slider.Fill, theme.Toolbar.Slider.Handle);

            IEnumerable<Slider> visualizationSliders = from element in themeElements where element.Type == ThemeElement.ElementType.VisualizationSlider select element.GetComponent<Slider>();
            foreach (Slider slider in visualizationSliders) SetSlider(slider, theme.Visualization.Slider.Background, theme.Visualization.Slider.Fill, theme.Visualization.Slider.Handle);
        }

        void SetButton(Button button, ColorBlock colorBlock)
        {
            if(button)
            {
                button.colors = colorBlock;
            }
        }
        void SetToggle(Toggle toggle, ColorBlock colorBlock, Color color)
        {
            if (toggle)
            {
                toggle.colors = colorBlock;
                toggle.graphic.color = color;
            }
        }
        void SetToggle(Toggle toggle, ColorBlock colorBlock)
        {
            if (toggle) toggle.colors = colorBlock;
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
                //text.alignment = fontData.alignment;
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
        void SetSlider(Slider slider, Color background, Color fill, Color handle)
        {
            if (slider)
            {
                Transform bg = slider.transform.Find("Background");
                if (bg) bg.GetComponent<Image>().color = background;
                if (slider.fillRect) slider.fillRect.GetComponent<Image>().color = fill;
                if (slider.handleRect) slider.handleRect.GetComponent<Image>().color = handle;
            }
        }
    }
}

