using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;
using System.Collections.Generic;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ItemModifier<d.Bloc> 
	{
		#region Properties
		InputField m_NameInputField, m_SortInputField, m_WindowMinInputField, m_WindowMaxInputField, m_BaselineMinInputField, m_BaselineMaxInputField;
		ImageSelector m_ImageFileSelector;
		EventList m_EventList;
        IconList m_IconList;
		Button m_SaveButton, m_AddEventButton, m_RemoveEventButton, m_AddIconButton, m_RemoveIconButton;

        [SerializeField] GameObject EventModifierPrefab;
        [SerializeField] GameObject IconModifierPrefab;
        List<EventModifier> m_EventModifiers = new List<EventModifier>();
        List<IconModifier> m_IconModifiers = new List<IconModifier>();
        #endregion

        #region Public Methods
        public override void Close()
        {
            foreach (var modifier in m_EventModifiers.ToArray()) modifier.Close();
            m_EventModifiers.Clear();
            foreach (var modifier in m_IconModifiers.ToArray()) modifier.Close();
            m_IconModifiers.Clear();
            base.Close();
        }
        public void AddEvent()
		{
            d.Event newEvent = new d.Event("Event", new int[] { 0 } ,d.Event.TypeEnum.Secondary);
            OpenEventModifier(newEvent);
        }
        public void RemoveEvent()
		{
            d.Event[] eventsToRemove = m_EventList.ObjectsSelected;
            foreach (d.Event e in eventsToRemove)
            {
                if (e.Type == d.Event.TypeEnum.Secondary)
                {
                    ItemTemp.Events.Remove(e);
                    m_EventList.Remove(e);
                }
            }
        }
        public void AddIcon()
        {
            d.Icon newIcon = new d.Icon("Icon","",new Vector2(ItemTemp.Window.Start,ItemTemp.Window.End));
            OpenIconModifier(newIcon);
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
            eventModifier.CloseEvent.AddListener(() => OnCloseEventModifier(eventModifier));
            m_EventModifiers.Add(eventModifier);
        }
        protected void OnSaveEventModifier(EventModifier eventModifier)
        {
            // Save
            if (!ItemTemp.Events.Contains(eventModifier.Item))
            {
                ItemTemp.Events.Add(eventModifier.Item);
                m_EventList.Add(eventModifier.Item);
            }
            else
            {
                m_EventList.UpdateObject(eventModifier.Item);
            }

            if(eventModifier.Item.Type == d.Event.TypeEnum.Main)
            {
                d.Event e = ItemTemp.Events.Find((ev) => ev.Type == d.Event.TypeEnum.Main && ev != eventModifier.Item);
                if (e != null)
                {
                    e.Type = d.Event.TypeEnum.Secondary;
                    m_EventList.UpdateObject(e);
                }
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
            iconModifier.CloseEvent.AddListener(() => OnCloseIconModifier(iconModifier));
            m_IconModifiers.Add(iconModifier);
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
        protected void OnCloseEventModifier(EventModifier modifier)
        {
            m_EventModifiers.Remove(modifier);
        }
        protected void OnCloseIconModifier(IconModifier modifier)
        {
            m_IconModifiers.Remove(modifier);
        }
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            m_ImageFileSelector.Path = objectToDisplay.IllustrationPath;
            m_ImageFileSelector.onValueChanged.AddListener(() => ItemTemp.IllustrationPath = m_ImageFileSelector.Path);

            m_SortInputField.text = objectToDisplay.Sort;
            m_SortInputField.onEndEdit.AddListener((value) => ItemTemp.Sort = value);

            m_WindowMinInputField.text = objectToDisplay.Window.Start.ToString();
            m_WindowMinInputField.onEndEdit.AddListener((value) => ItemTemp.Window = new Tools.CSharp.Window(float.Parse(value), ItemTemp.Window.End));

            m_WindowMaxInputField.text = objectToDisplay.Window.End.ToString();
            m_WindowMaxInputField.onEndEdit.AddListener((value) => ItemTemp.Window = new Tools.CSharp.Window(ItemTemp.Window.Start, float.Parse(value)));

            m_BaselineMinInputField.text = objectToDisplay.Baseline.Start.ToString();
            m_BaselineMinInputField.onEndEdit.AddListener((value) => ItemTemp.Baseline = new Tools.CSharp.Window(float.Parse(value),ItemTemp.Baseline.End));

            m_BaselineMaxInputField.text = objectToDisplay.Baseline.End.ToString();
            m_BaselineMaxInputField.onEndEdit.AddListener((value) => ItemTemp.Baseline = new Tools.CSharp.Window(ItemTemp.Baseline.Start, float.Parse(value)));

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
            m_BaselineMinInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Baseline").Find("Panel").Find("Min").GetComponentInChildren<InputField>();
            m_BaselineMaxInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Baseline").Find("Panel").Find("Max").GetComponentInChildren<InputField>();

            // Events.
            m_EventList = transform.Find("Content").Find("Events").Find("List").Find("List").Find("Display").GetComponent<EventList>();
            m_AddEventButton = transform.Find("Content").Find("Events").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveEventButton = transform.Find("Content").Find("Events").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();

            // Icons.
            m_IconList = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("List").Find("Display").GetComponent<IconList>();
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
            m_BaselineMinInputField.interactable = interactable;
            m_BaselineMaxInputField.interactable = interactable;
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