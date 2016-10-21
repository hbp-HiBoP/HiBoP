using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Skin
{
    [ExecuteInEditMode]
    public class MenuSkinGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        Image[] m_MenuBG;

        [SerializeField]
        Image[] m_MenuSeparators;

        [SerializeField]
        Button[] m_MenuButtons;

        [SerializeField]
        Image[] m_WindowBG;

        [SerializeField]
        Image[] m_TitleBG;

        [SerializeField]
        Text[] m_Titles;

        [SerializeField]
        Text[] m_Labels;

        [SerializeField]
        Button[] m_MainButtons;

        [SerializeField]
        Button[] m_SecondaryButtons;

        [SerializeField]
        InputField[] m_InputFields;

        [SerializeField]
        ScrollRect[] m_scrollRects;

        [SerializeField]
        Scrollbar[] m_scrollBars;

        [SerializeField]
        Toggle[] m_toggles;

        [SerializeField]
        Dropdown[] m_dropDowns;
        #endregion

        #region Methods
        public void Set(Skin skin)
        {
            // Menu.
            foreach (Image bg in m_MenuBG)
            {
                bg.color = skin.Color.MenuBG;
            }

            foreach (Image sep in m_MenuSeparators)
            {
                sep.color = skin.Color.MenuSeparator;
            }

            foreach (Button b in m_MenuButtons)
            {
                b.targetGraphic.color = skin.Color.MenuButtonHighLightedBG;
                ButtonGestion buttonGestion = b.GetComponent<ButtonGestion>();
                if (buttonGestion != null)
                {
                    buttonGestion.InteractableColor = skin.Color.MenuInteractableF;
                    buttonGestion.NotInteractableColor = skin.Color.MenuNotInteractableF;
                }
            }

            // Window.
            foreach (Button b in m_MainButtons)
            {
                b.colors = skin.Color.MainButton;
                Text[] l_text = b.GetComponentsInChildren<Text>();
                foreach (Text t in l_text)
                {
                    t.color = skin.Color.MainButtonFont;
                }
                Image[] l_image = b.GetComponentsInChildren<Image>();
                foreach(Image i in l_image)
                {
                    if (i != b.targetGraphic)
                    {
                        i.color = skin.Color.MainButtonFont;
                    }
                }
            }

            foreach (Button b in m_SecondaryButtons)
            {
                b.colors = skin.Color.SecondaryButton;
                Text[] l_text = b.GetComponentsInChildren<Text>();
                foreach (Text t in l_text)
                {
                    t.color = skin.Color.SecondaryButtonFont;
                }
                Image[] l_image = b.GetComponentsInChildren<Image>();
                foreach (Image i in l_image)
                {
                    i.color = skin.Color.SecondaryButtonFont;
                }
            }

            foreach (Image bg in m_WindowBG)
            {
                bg.color = skin.Color.WindowBG;
            }

            foreach (Image bg in m_TitleBG)
            {
                bg.color = skin.Color.TitleBG;
            }

            foreach (InputField i in m_InputFields)
            {
                i.colors = skin.Color.InputField;
                Text[] l_text = i.GetComponentsInChildren<Text>();
                foreach (Text t in l_text)
                {
                    t.color = skin.Color.InputFieldFont;
                }
            }

            foreach (Text t in m_Labels)
            {
                t.color = skin.Color.WindowFont;
            }

            foreach (Text t in m_Titles)
            {
                t.color = skin.Color.TitleFont;
            }

            foreach(ScrollRect s in m_scrollRects)
            {
                s.GetComponent<Image>().color = skin.Color.ScroolViewBG;
            }

            foreach(Scrollbar s in m_scrollBars)
            {
                s.colors = skin.Color.ScrollBar;
                s.GetComponent<Image>().color = skin.Color.ScrollBarBG;
            }

            foreach(Toggle t in m_toggles)
            {
                t.colors = skin.Color.Toggle;
                t.graphic.color = skin.Color.CheckMark;
            }

            foreach(Dropdown d in m_dropDowns)
            {
                d.colors = skin.Color.Dropdown;
                d.itemText.color = skin.Color.DropdownFont;
                d.captionText.color = skin.Color.DropdownFont;
                d.itemText.transform.parent.GetComponent<Toggle>().colors = skin.Color.Dropdown;
                d.template.GetComponent<Image>().color = skin.Color.Dropdown.normalColor;
            }
        }
        #endregion
    }
}

