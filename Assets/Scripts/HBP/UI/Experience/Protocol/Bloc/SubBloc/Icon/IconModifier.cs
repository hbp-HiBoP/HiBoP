using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class IconModifier : ItemModifier<d.Icon>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_MinInputField;
        [SerializeField] InputField m_MaxInputField;
        [SerializeField] ImageSelector m_ImageSelector;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_MinInputField.interactable = value;
                m_MaxInputField.interactable = value;
                m_ImageSelector.interactable = value;
            }
        }
        #endregion

        protected override void SetFields(d.Icon objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_MinInputField.text = objectToDisplay.Window.Start.ToString();
            m_MinInputField.onValueChanged.AddListener((min) => ItemTemp.Window = new Tools.CSharp.Window(int.Parse(min), ItemTemp.Window.End));
            m_MaxInputField.text = objectToDisplay.Window.End.ToString();
            m_MaxInputField.onValueChanged.AddListener((max) => ItemTemp.Window = new Tools.CSharp.Window(ItemTemp.Window.Start, int.Parse(max)));
            m_ImageSelector.Path = objectToDisplay.IllustrationPath;
            m_ImageSelector.onValueChanged.AddListener(() => ItemTemp.IllustrationPath = m_ImageSelector.Path);
        }
    }
}