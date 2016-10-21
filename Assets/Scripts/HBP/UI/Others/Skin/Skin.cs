using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Skin
{
    public class Skin
    {
        public ColorSkin Color;

        public Skin(ColorSkin color)
        {
            Color = color;
        }

        public Skin() : this(new ColorSkin())
        {
        }
    }

    public class ColorSkin
    {
        // Menu.
        public Color MenuBG;
        public Color MenuSeparator;
        public Color MenuInteractableF;
        public Color MenuNotInteractableF;
        public Color MenuButtonHighLightedBG;

        // Widnow.
        public Color WindowBG;
        public Color TitleBG;
        public Color TitleFont;
        public Color WindowFont;

        // InputField.
        public ColorBlock InputField;
        public Color InputFieldFont;

        // Main Buttons.
        public ColorBlock MainButton;
        public Color MainButtonFont;

        // SecondaryButtons.
        public ColorBlock SecondaryButton;
        public Color SecondaryButtonFont;

        // ScrollBar.
        public ColorBlock ScrollBar;
        public Color ScrollBarBG;

        // ScrolLViewBG.
        public Color ScroolViewBG;

        // SelectAllToggle
        public ColorBlock Toggle;
        public Color CheckMark;

        // Dropdown.
        public ColorBlock Dropdown;
        public Color DropdownFont;

        public ColorSkin()
        {
            // Menu.
            MenuBG = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255f / 255f);
            MenuSeparator = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            MenuInteractableF = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            MenuNotInteractableF = new Color(100f / 255f, 100f / 255f, 100f / 255f, 255f / 255f);
            MenuButtonHighLightedBG = new Color(40 / 255f, 40 / 255f, 40 / 255f, 255f / 255f);

            // Window.
            WindowBG = new Color(60 / 255f, 60 / 255f, 60 / 255f, 255f / 255f);
            TitleBG = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            TitleFont = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            WindowFont = new Color(255f / 255f, 255 / 255f, 255 / 255f, 255f / 255f);

            // InputField.
            InputField = new ColorBlock();
            InputField.normalColor = new Color(100 / 255f, 100 / 255f, 100 / 255f, 255f / 255f);
            InputField.highlightedColor = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            InputField.pressedColor = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255f / 255f);
            InputField.disabledColor = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255f / 255f);
            InputField.fadeDuration = 0.1f;
            InputField.colorMultiplier = 1;
            InputFieldFont = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);

            // Main Button.
            MainButton = new ColorBlock();
            MainButton.normalColor = new Color(255 / 255f, 255 / 255f, 255 / 255f, 255f / 255f);
            MainButton.highlightedColor = new Color(220 / 255f, 220 / 255f, 220 / 255f, 255f / 255f);
            MainButton.pressedColor = new Color(200 / 255f, 200 / 255f, 200 / 255f, 255f / 255f);
            MainButton.disabledColor = new Color(200 / 255f, 200 / 255f, 200 / 255f, 128 / 255f);
            MainButton.fadeDuration = 0.1f;
            MainButton.colorMultiplier = 1;
            MainButtonFont = new Color(0 / 255f, 0 / 255f, 0 / 255f, 255f / 255f);

            // Secondary Button.
            SecondaryButton = new ColorBlock();
            SecondaryButton.normalColor = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            SecondaryButton.highlightedColor = new Color(70 / 255f, 70 / 255f, 70 / 255f, 255f / 255f);
            SecondaryButton.pressedColor = new Color(60 / 255f, 60 / 255f, 60 / 255f, 255f / 255f);
            SecondaryButton.disabledColor = new Color(200 / 255f, 200 / 255f, 200 / 255f, 128 / 255f);
            SecondaryButton.fadeDuration = 0.1f;
            SecondaryButton.colorMultiplier = 1;
            SecondaryButtonFont = new Color(255/ 255f, 255 / 255f, 255 / 255f, 255f / 255f);

            // ScrollBar.
            ScrollBar = new ColorBlock();
            ScrollBar.normalColor = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            ScrollBar.highlightedColor = new Color(70 / 255f, 70 / 255f, 70 / 255f, 255f / 255f);
            ScrollBar.pressedColor = new Color(60 / 255f, 60 / 255f, 60 / 255f, 255f / 255f);
            ScrollBar.disabledColor = new Color(200 / 255f, 200 / 255f, 200 / 255f, 128 / 255f);
            ScrollBar.fadeDuration = 0.1f;
            ScrollBar.colorMultiplier = 1;
            ScrollBarBG = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255f / 255f);

            // ScrolLViewBG.
            ScroolViewBG = new Color(100 / 255f, 100 / 255f, 100 / 255f, 255f / 255f);

            // SelectAllToggle
            Toggle = new ColorBlock();
            Toggle.normalColor = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            Toggle.highlightedColor = new Color(70 / 255f, 70 / 255f, 70 / 255f, 255f / 255f);
            Toggle.pressedColor = new Color(60 / 255f, 60 / 255f, 60 / 255f, 255f / 255f);
            Toggle.disabledColor = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255 / 255f);
            Toggle.fadeDuration = 0.1f;
            Toggle.colorMultiplier = 1; ;
            CheckMark = new Color(59/255f,122/255f,194/255f,255/255f);

            // Dropdown.
            Dropdown = new ColorBlock();
            Dropdown.normalColor = new Color(100 / 255f, 100 / 255f, 100 / 255f, 255f / 255f);
            Dropdown.highlightedColor = new Color(80 / 255f, 80 / 255f, 80 / 255f, 255f / 255f);
            Dropdown.pressedColor = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255f / 255f);
            Dropdown.disabledColor = new Color(50 / 255f, 50 / 255f, 50 / 255f, 255f / 255f);
            Dropdown.fadeDuration = 0.1f;
            Dropdown.colorMultiplier = 1;
            DropdownFont = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        }
    }

    public class SizeSkin
    {
        public int MenuFont;
        public int TitleFont;
        public int WindowFont;

        public SizeSkin(int menuFont)
        {
            MenuFont = menuFont;
        }

        public SizeSkin()
        {
            TitleFont = 15;
            MenuFont = 14;
            WindowFont = 14;
        }
    }
}