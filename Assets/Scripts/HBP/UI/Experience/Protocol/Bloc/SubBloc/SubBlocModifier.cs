using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocModifier : ItemModifier<d.SubBloc>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PositionInputField;
        [SerializeField] Slider m_StartWindowSlider;
        [SerializeField] Slider m_EndWindowSlider;
        [SerializeField] Slider m_StartBaselineSlider;
        [SerializeField] Slider m_EndBaselineSlider;

        [SerializeField] EventGestion m_EventGestion;
        [SerializeField] IconGestion m_IconGestion;
        [SerializeField] TreatmentGestion m_TreatmentGestion;

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
                m_PositionInputField.interactable = value;

                m_StartWindowSlider.interactable = value;
                m_EndWindowSlider.interactable = value;

                m_StartBaselineSlider.interactable = value;
                m_EndBaselineSlider.interactable = value;

                m_EventGestion.interactable = value;
                m_IconGestion.interactable = value;
                m_TreatmentGestion.interactable = value;
            }
        }

        #endregion

        #region Private Methods
        protected override void SetFields(d.SubBloc objectToDisplay)
        {
            // General
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            m_PositionInputField.text = objectToDisplay.Position.ToString();
            m_PositionInputField.onEndEdit.AddListener((value) => ItemTemp.Position = int.Parse(value));

            m_StartWindowSlider.value = objectToDisplay.Window.Start;
            m_StartWindowSlider.onValueChanged.AddListener((value) => ItemTemp.Window = new Tools.CSharp.Window(value,ItemTemp.Window.End));

            m_EndWindowSlider.value = objectToDisplay.Window.End;
            m_EndWindowSlider.onValueChanged.AddListener((value) => ItemTemp.Window = new Tools.CSharp.Window(ItemTemp.Window.Start, value));

            m_StartBaselineSlider.value = objectToDisplay.Baseline.Start;
            m_StartBaselineSlider.onValueChanged.AddListener((value) => ItemTemp.Baseline = new Tools.CSharp.Window(value, ItemTemp.Baseline.End));

            m_EndBaselineSlider.value = objectToDisplay.Baseline.End;
            m_EndBaselineSlider.onValueChanged.AddListener((value) => ItemTemp.Baseline = new Tools.CSharp.Window(ItemTemp.Baseline.Start, value));

            // Event
            m_EventGestion.Set(objectToDisplay);

            // Icon
            m_IconGestion.Set(objectToDisplay);

            // Treatment
            m_TreatmentGestion.Set(objectToDisplay);
        }
        #endregion
    }
}