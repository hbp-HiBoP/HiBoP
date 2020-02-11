using HBP.Data.Preferences;
using Tools.CSharp;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocModifier : ObjectModifier<d.SubBloc>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_OrderInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] RangeSlider m_WindowSlider;
        [SerializeField] RangeSlider m_BaselineSlider;

        [SerializeField] EventListGestion m_EventListGestion;
        [SerializeField] IconListGestion m_IconListGestion;
        [SerializeField] TreatmentListGestion m_TreatmentListGestion;

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
                m_OrderInputField.interactable = value;
                m_TypeDropdown.interactable = value && ItemTemp != null && ItemTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;
                m_WindowSlider.interactable = value;
                m_BaselineSlider.interactable = value;

                m_EventListGestion.Interactable = value;
                m_EventListGestion.Modifiable = value;

                m_IconListGestion.Interactable = value;
                m_IconListGestion.Modifiable = value;

                m_TreatmentListGestion.Interactable = value;
                m_TreatmentListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_OrderInputField.onEndEdit.AddListener(OnChangeOrder);
            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);

            m_WindowSlider.onValueChanged.AddListener(OnChangeWindow);
            m_BaselineSlider.onValueChanged.AddListener(OnChangeBaseline);

            m_EventListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_EventListGestion.List.OnAddObject.AddListener(OnAddEvent);
            m_EventListGestion.List.OnRemoveObject.AddListener(OnRemoveEvent);
            m_EventListGestion.List.OnUpdateObject.AddListener(OnUpdateEvent);

            m_IconListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_IconListGestion.List.OnAddObject.AddListener(OnAddIcon);
            m_IconListGestion.List.OnRemoveObject.AddListener(OnRemoveIcon);
            m_IconListGestion.List.OnUpdateObject.AddListener(OnUpdateIcon);

            m_TreatmentListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_TreatmentListGestion.List.OnAddObject.AddListener(OnAddTreatment);
            m_TreatmentListGestion.List.OnRemoveObject.AddListener(OnRemoveTreatment);
            m_TreatmentListGestion.List.OnUpdateObject.AddListener(OnUpdateTreatment);
        }
        protected override void SetFields(d.SubBloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_TypeDropdown.Set(typeof(Data.Enums.MainSecondaryEnum), (int)objectToDisplay.Type);
            m_TypeDropdown.interactable = m_Interactable && ItemTemp != null && ItemTemp.Type == Data.Enums.MainSecondaryEnum.Secondary;

            ProtocolPreferences preferences = ApplicationState.UserPreferences.Data.Protocol;
            m_WindowSlider.minLimit = preferences.MinLimit;
            m_WindowSlider.maxLimit = preferences.MaxLimit;
            m_WindowSlider.step = preferences.Step;
            m_WindowSlider.Values = objectToDisplay.Window.ToVector2();

            m_BaselineSlider.minLimit = preferences.MinLimit;
            m_BaselineSlider.maxLimit = preferences.MaxLimit;
            m_BaselineSlider.step = preferences.Step;
            m_BaselineSlider.Values = objectToDisplay.Baseline.ToVector2();

            m_EventListGestion.List.Set(objectToDisplay.Events);
            m_IconListGestion.List.Set(objectToDisplay.Icons);
            m_TreatmentListGestion.List.Set(objectToDisplay.Treatments);
        }

        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        protected void OnChangeOrder(string value)
        {
            if (int.TryParse(value, out int order))
            {
                ItemTemp.Order = order;
            }
            else
            {
                m_OrderInputField.text = ItemTemp.Order.ToString();
            }
        }
        protected void OnChangeType(int value)
        {
            ItemTemp.Type = (Data.Enums.MainSecondaryEnum)value;
        }
        protected void OnChangeWindow(float min, float max)
        {
            ItemTemp.Window = new Tools.CSharp.Window(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            m_IconListGestion.Window = itemTemp.Window;
            m_TreatmentListGestion.Window = itemTemp.Window;
            m_TreatmentListGestion.Baseline = itemTemp.Baseline;
        }
        protected void OnChangeBaseline(float min, float max)
        {
            ItemTemp.Baseline = new Tools.CSharp.Window(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            m_TreatmentListGestion.Baseline = itemTemp.Baseline;
        }

        protected void OnAddEvent(d.Event @event)
        {
            ItemTemp.Events.AddIfAbsent(@event);
        }
        protected void OnRemoveEvent(d.Event @event)
        {
            ItemTemp.Events.Remove(@event);
        }
        protected void OnUpdateEvent(d.Event @event)
        {
            int index = ItemTemp.Events.FindIndex(t => t.Equals(@event));
            if (index != -1)
            {
                ItemTemp.Events[index] = @event;
            }
        }

        protected void OnAddIcon(d.Icon icon)
        {
            ItemTemp.Icons.AddIfAbsent(icon);
        }
        protected void OnRemoveIcon(d.Icon icon)
        {
            ItemTemp.Icons.Remove(icon);
        }
        protected void OnUpdateIcon(d.Icon icon)
        {
            int index = ItemTemp.Icons.FindIndex(i => i.Equals(icon));
            if (index != -1)
            {
                ItemTemp.Icons[index] = icon;
            }
        }

        protected void OnAddTreatment(d.Treatment treatment)
        {
            ItemTemp.Treatments.AddIfAbsent(treatment);
        }
        protected void OnRemoveTreatment(d.Treatment treatment)
        {
            ItemTemp.Treatments.Remove(treatment);
        }
        protected void OnUpdateTreatment(d.Treatment treatment)
        {
            int index = ItemTemp.Treatments.FindIndex(t => t.Equals(treatment));
            if(index != -1)
            {
                ItemTemp.Treatments[index] = treatment;
            }
        }
        #endregion
    }
}