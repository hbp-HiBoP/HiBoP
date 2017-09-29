using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ItemModifier<d.Bloc> 
	{
		#region Properties
		InputField m_NameInputField, m_SortInputField, m_WindowMinInputField, m_WindowMaxInputField, m_BaseLineMinInputField, m_BaseLineMaxInputField;
		ImageSelector m_ImageFileSelector;
		EventList m_EventList;
        IconList m_IconList;
		Button m_SaveButton, m_AddEventButton, m_RemoveEventButton, m_AddIconButton, m_RemoveIconButton;

        [SerializeField] GameObject EventModifierPrefab;
        [SerializeField] GameObject IconModifierPrefab;
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
        }
        public void AddSecondaryEvent()
		{
            d.Event newEvent = new d.Event("Event", new int[0] ,d.Event.TypeEnum.Secondary);
            ItemTemp.Events.Add(newEvent);
            m_EventList.Add(newEvent);
        }
        public void RemoveSecondaryEvent()
		{
            d.Event[] eventsToRemove = m_EventList.ObjectsSelected;
            foreach(d.Event e in eventsToRemove) ItemTemp.Events.Remove(e);
            m_EventList.Remove(eventsToRemove);
        }
        public void AddIcon()
        {
            d.Icon newIcon = new d.Icon();
            ItemTemp.Scenario.Icons.Add(newIcon);
            m_IconList.Add(newIcon);
        }
        public void RemoveIcon()
        {
            d.Icon[] iconsToRemove = m_IconList.ObjectsSelected;
            foreach(d.Icon i in iconsToRemove) ItemTemp.Scenario.Icons.Remove(i);
            m_IconList.Remove(iconsToRemove);
        }
        #endregion

        #region Private Methods
        protected void OpenEventModifier(d.Event event_)
        {
            RectTransform obj = Instantiate(EventModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            EventModifier eventModifier = obj.GetComponent<EventModifier>();
            eventModifier.Open(event_, true);
            eventModifier.SaveEvent.AddListener(() => OnSaveEventModifier(eventModifier));
        }
        protected void OnSaveEventModifier(EventModifier eventModifier)
        {
            if (!ItemTemp.Events.Contains(eventModifier.Item))
            {
                ItemTemp.Events.Add(eventModifier.Item);
                m_EventList.Add(eventModifier.Item);
            }
            else
            {
                m_EventList.UpdateObject(eventModifier.Item);
            }
        }
        protected void OpenIconModifier(d.Icon icon)
        {
            RectTransform obj = Instantiate(IconModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            IconModifier iconModifier = obj.GetComponent<IconModifier>();
            iconModifier.Open(icon, true);
            iconModifier.SaveEvent.AddListener(() => OnSaveIconModifier(iconModifier));
        }
        protected void OnSaveIconModifier(IconModifier iconModifier)
        {
            if (!ItemTemp.Scenario.Icons.Contains(iconModifier.Item))
            {
                ItemTemp.Scenario.Icons.Add(iconModifier.Item);
                m_IconList.Add(iconModifier.Item);
            }
            else
            {
                m_IconList.UpdateObject(iconModifier.Item);
            }
        }
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.DisplayInformations.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Name = value);

            m_ImageFileSelector.Path = objectToDisplay.DisplayInformations.IllustrationPath;
            m_ImageFileSelector.onValueChanged.AddListener(() => ItemTemp.DisplayInformations.IllustrationPath = m_ImageFileSelector.Path);

            m_SortInputField.text = objectToDisplay.DisplayInformations.Sort;
            m_SortInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Sort = value);

            m_WindowMinInputField.text = objectToDisplay.DisplayInformations.Window.Start.ToString();
            m_WindowMinInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Window = new Tools.CSharp.Window(float.Parse(value), ItemTemp.DisplayInformations.Window.End));

            m_WindowMaxInputField.text = objectToDisplay.DisplayInformations.Window.End.ToString();
            m_WindowMaxInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Window = new Tools.CSharp.Window(ItemTemp.DisplayInformations.Window.Start, float.Parse(value)));

            m_BaseLineMinInputField.text = objectToDisplay.DisplayInformations.BaseLine.Start.ToString();
            m_BaseLineMinInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.BaseLine = new Tools.CSharp.Window(float.Parse(value),ItemTemp.DisplayInformations.BaseLine.End));

            m_BaseLineMaxInputField.text = objectToDisplay.DisplayInformations.BaseLine.End.ToString();
            m_BaseLineMaxInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.BaseLine = new Tools.CSharp.Window(ItemTemp.DisplayInformations.BaseLine.Start, float.Parse(value)));

            m_EventList.Objects = ItemTemp.Events.ToArray();
            m_EventList.OnAction.AddListener((e, i) => OpenEventModifier(e));
            m_IconList.Objects = ItemTemp.Scenario.Icons.ToArray();
            m_IconList.OnAction.AddListener((icon, i) => OpenIconModifier(icon));
        }
        protected override void SetWindow()
        {
            // General.
            m_NameInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Name").GetComponentInChildren<InputField>();
            m_ImageFileSelector = transform.Find("Content").Find("General").Find("Fields").Find("Right").GetComponentInChildren<ImageSelector>();
            m_SortInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Sort").GetComponentInChildren<InputField>();
            m_WindowMinInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Window").Find("Panel").Find("Min").GetComponentInChildren<InputField>();
            m_WindowMaxInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Window").Find("Panel").Find("Max").GetComponentInChildren<InputField>();
            m_BaseLineMinInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("BaseLine").Find("Panel").Find("Min").GetComponentInChildren<InputField>();
            m_BaseLineMaxInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("BaseLine").Find("Panel").Find("Max").GetComponentInChildren<InputField>();

            // Events.
            m_EventList = transform.Find("Content").Find("Events").Find("List").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<EventList>();
            m_AddEventButton = transform.Find("Content").Find("Events").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveEventButton = transform.Find("Content").Find("Events").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();

            // Icons.
            m_IconList = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<IconList>();
            m_AddIconButton = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveIconButton = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();

            // Others.
            m_SaveButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();

        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_ImageFileSelector.interactable = interactable;
            m_SortInputField.interactable = interactable;
            m_WindowMinInputField.interactable = interactable;
            m_WindowMaxInputField.interactable = interactable;
            m_BaseLineMinInputField.interactable = interactable;
            m_BaseLineMaxInputField.interactable = interactable;
            m_ImageFileSelector.interactable = interactable;
            m_SaveButton.interactable = interactable;
            m_AddEventButton.interactable = interactable;
            m_RemoveEventButton.interactable = interactable;
            m_AddIconButton.interactable = interactable;
            m_RemoveIconButton.interactable = interactable;
            m_IconList.interactable = interactable;
            m_EventList.interactable = interactable;
        }
        #endregion
    }
}