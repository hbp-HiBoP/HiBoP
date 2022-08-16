using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Display.Preferences;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Window to modify a subBloc.
    /// </summary>
    public class SubBlocModifier : ObjectModifier<SubBloc>
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

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
                m_TypeDropdown.interactable = value && ObjectTemp != null && ObjectTemp.Type == MainSecondaryEnum.Secondary;
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
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_OrderInputField.onEndEdit.AddListener(ChangeOrder);
            m_TypeDropdown.onValueChanged.AddListener(ChangeType);

            m_WindowSlider.onValueChanged.AddListener(ChangeWindow);
            m_BaselineSlider.onValueChanged.AddListener(ChangeBaseline);

            m_EventListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_EventListGestion.List.OnAddObject.AddListener(AddEvent);
            m_EventListGestion.List.OnRemoveObject.AddListener(RemoveEvent);
            m_EventListGestion.List.OnUpdateObject.AddListener(UpdateEvent);

            m_IconListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_IconListGestion.List.OnAddObject.AddListener(AddIcon);
            m_IconListGestion.List.OnRemoveObject.AddListener(RemoveIcon);
            m_IconListGestion.List.OnUpdateObject.AddListener(UpdateIcon);

            m_TreatmentListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_TreatmentListGestion.List.OnAddObject.AddListener(AddTreatment);
            m_TreatmentListGestion.List.OnRemoveObject.AddListener(RemoveTreatment);
            m_TreatmentListGestion.List.OnUpdateObject.AddListener(UpdateTreatment);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">SubBloc to display</param>
        protected override void SetFields(SubBloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_TypeDropdown.Set(typeof(MainSecondaryEnum), (int)objectToDisplay.Type);
            m_TypeDropdown.interactable = m_Interactable && ObjectTemp != null && ObjectTemp.Type == MainSecondaryEnum.Secondary;

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
        /// <summary>
        /// Change the name.
        /// </summary>
        /// <param name="value">Name</param>
        protected void ChangeName(string value)
        {
            if(value != "")
            {
                ObjectTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Change the order.
        /// </summary>
        /// <param name="value">Order</param>
        protected void ChangeOrder(string value)
        {
            if (int.TryParse(value, out int order))
            {
                ObjectTemp.Order = order;
            }
            else
            {
                m_OrderInputField.text = ObjectTemp.Order.ToString();
            }
        }
        /// <summary>
        /// Change the type.
        /// </summary>
        /// <param name="value">Type</param>
        protected void ChangeType(int value)
        {
            ObjectTemp.Type = (MainSecondaryEnum)value;
        }
        /// <summary>
        /// Change the window.
        /// </summary>
        /// <param name="min">Start window</param>
        /// <param name="max">End window</param>
        protected void ChangeWindow(float min, float max)
        {
            ObjectTemp.Window = new Core.Tools.TimeWindow(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            m_IconListGestion.Window = m_ObjectTemp.Window;
            m_TreatmentListGestion.Window = m_ObjectTemp.Window;
            m_TreatmentListGestion.Baseline = m_ObjectTemp.Baseline;
        }
        /// <summary>
        /// Change the baseline.
        /// </summary>
        /// <param name="min">Start window</param>
        /// <param name="max">End window</param>
        protected void ChangeBaseline(float min, float max)
        {
            ObjectTemp.Baseline = new Core.Tools.TimeWindow(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            m_TreatmentListGestion.Baseline = m_ObjectTemp.Baseline;
        }
        /// <summary>
        /// Add event to the subBloc.
        /// </summary>
        /// <param name="event">Event to add</param>
        protected void AddEvent(Core.Data.Event @event)
        {
            ObjectTemp.Events.AddIfAbsent(@event);
        }
        /// <summary>
        /// Remove event to the subBloc.
        /// </summary>
        /// <param name="event">Event to remove</param>
        protected void RemoveEvent(Core.Data.Event @event)
        {
            ObjectTemp.Events.Remove(@event);
        }
        /// <summary>
        /// Update event to the subBloc.
        /// </summary>
        /// <param name="event"></param>
        protected void UpdateEvent(Core.Data.Event @event)
        {
            int index = ObjectTemp.Events.FindIndex(t => t.Equals(@event));
            if (index != -1)
            {
                ObjectTemp.Events[index] = @event;
            }
        }
        /// <summary>
        /// Add icon to the subBloc.
        /// </summary>
        /// <param name="icon">Icon to add</param>
        protected void AddIcon(Icon icon)
        {
            ObjectTemp.Icons.AddIfAbsent(icon);
        }
        /// <summary>
        /// Remove icon from the subBloc.
        /// </summary>
        /// <param name="icon">Icon to remove</param>
        protected void RemoveIcon(Icon icon)
        {
            ObjectTemp.Icons.Remove(icon);
        }
        /// <summary>
        /// Update icon from the subBloc.
        /// </summary>
        /// <param name="icon">Icon to update</param>
        protected void UpdateIcon(Icon icon)
        {
            int index = ObjectTemp.Icons.FindIndex(i => i.Equals(icon));
            if (index != -1)
            {
                ObjectTemp.Icons[index] = icon;
            }
        }
        /// <summary>
        /// Add treatment to the subBloc.
        /// </summary>
        /// <param name="treatment">Treatment to add</param>
        protected void AddTreatment(Treatment treatment)
        {
            ObjectTemp.Treatments.AddIfAbsent(treatment);
        }
        /// <summary>
        /// Remove treatment from the subBloc.
        /// </summary>
        /// <param name="treatment">Treatment to remove</param>
        protected void RemoveTreatment(Treatment treatment)
        {
            ObjectTemp.Treatments.Remove(treatment);
        }
        /// <summary>
        /// Update treatment to the subBloc.
        /// </summary>
        /// <param name="treatment">Treatment to update</param>
        protected void UpdateTreatment(Treatment treatment)
        {
            int index = ObjectTemp.Treatments.FindIndex(t => t.Equals(treatment));
            if(index != -1)
            {
                ObjectTemp.Treatments[index] = treatment;
            }
        }
        #endregion
    }
}