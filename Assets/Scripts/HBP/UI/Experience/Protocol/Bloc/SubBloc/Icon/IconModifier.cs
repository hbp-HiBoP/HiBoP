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
        [SerializeField] Slider m_StartWindowSlider;
        [SerializeField] Slider m_EndWindowSlider;
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
                m_StartWindowSlider.interactable = value;
                m_EndWindowSlider.interactable = value;
                m_ImageSelector.interactable = value;
            }
        }
        #endregion

        protected override void SetFields(d.Icon objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_StartWindowSlider.value = objectToDisplay.Window.Start;
            m_StartWindowSlider.onValueChanged.AddListener((min) => ItemTemp.Window = new Tools.CSharp.Window((int) min, ItemTemp.Window.End));
            m_EndWindowSlider.value = objectToDisplay.Window.End;
            m_EndWindowSlider.onValueChanged.AddListener((max) => ItemTemp.Window = new Tools.CSharp.Window(ItemTemp.Window.Start, (int) max));
            m_ImageSelector.Path = objectToDisplay.IllustrationPath;
            m_ImageSelector.onValueChanged.AddListener(() => ItemTemp.IllustrationPath = m_ImageSelector.Path);
        }
    }
}