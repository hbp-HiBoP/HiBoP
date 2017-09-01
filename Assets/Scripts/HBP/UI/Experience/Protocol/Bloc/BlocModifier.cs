using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ItemModifier<d.Bloc> 
	{
		#region Properties
		InputField m_NameInputField, m_SortInputField, m_WindowMinInputField, m_WindowMaxInputField, m_BaseLineMinInputField, m_BaseLineMaxInputField, m_MainEventLabelInputField, m_MainEventCodesInputField;
		FileSelector m_ImageFileSelector;
		EventList m_EventList;
        IconList m_IconList;
		Button m_SaveButton, m_AddEventButton, m_RemoveEventButton, m_AddIconButton, m_RemoveIconButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            m_IconList.SaveAll();
            m_EventList.SaveAll();
            base.Save();
        }
        public void AddSecondaryEvent()
		{
            d.Event newEvent = new d.Event();
            ItemTemp.SecondaryEvents.Add(newEvent);
            m_EventList.Add(newEvent);
        }
        public void RemoveSecondaryEvent()
		{
            d.Event[] eventsToRemove = m_EventList.ObjectsSelected;
            foreach(d.Event e in eventsToRemove) ItemTemp.SecondaryEvents.Remove(e);
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
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.DisplayInformations.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Name = value);

            m_ImageFileSelector.File = objectToDisplay.DisplayInformations.IllustrationPath;
            m_ImageFileSelector.onValueChanged.AddListener((value) => ItemTemp.DisplayInformations.IllustrationPath = value);

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

            m_MainEventLabelInputField.text = objectToDisplay.MainEvent.Name;
            m_MainEventLabelInputField.onEndEdit.AddListener((value) => ItemTemp.MainEvent.Name = value);

            m_MainEventCodesInputField.text = objectToDisplay.MainEvent.CodesString;
            m_MainEventCodesInputField.onEndEdit.AddListener((value) => ItemTemp.MainEvent.CodesString = value);

            m_EventList.Objects = ItemTemp.SecondaryEvents.ToArray();
            m_IconList.Objects = ItemTemp.Scenario.Icons.ToArray();
        }
        protected override void SetWindow()
        {
            m_NameInputField = transform.Find("Content").Find("Display informations").Find("Name").Find("InputField").GetComponent<InputField>();
            m_ImageFileSelector = transform.Find("Content").Find("Display informations").Find("Illustration").Find("FileSelector").GetComponent<FileSelector>();
            m_SortInputField = transform.Find("Content").Find("Display informations").Find("Sort").Find("InputField").GetComponent<InputField>();
            m_WindowMinInputField = transform.Find("Content").Find("Display informations").Find("Window").Find("Panel").Find("Min").Find("InputField").GetComponent<InputField>();
            m_WindowMaxInputField = transform.Find("Content").Find("Display informations").Find("Window").Find("Panel").Find("Max").Find("InputField").GetComponent<InputField>();
            m_BaseLineMinInputField = transform.Find("Content").Find("Display informations").Find("BaseLine").Find("Panel").Find("Min").Find("InputField").GetComponent<InputField>();
            m_BaseLineMaxInputField = transform.Find("Content").Find("Display informations").Find("BaseLine").Find("Panel").Find("Max").Find("InputField").GetComponent<InputField>();
            m_MainEventLabelInputField = transform.Find("Content").Find("Main Event").Find("Label").Find("InputField").GetComponent<InputField>();
            m_MainEventCodesInputField = transform.Find("Content").Find("Main Event").Find("Code").Find("InputField").GetComponent<InputField>();
            m_EventList = transform.Find("Content").Find("Secondary Events").Find("List").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<EventList>();
            m_IconList = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<IconList>();
            m_SaveButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
            m_AddEventButton = transform.Find("Content").Find("Secondary Events").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveEventButton = transform.Find("Content").Find("Secondary Events").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();
            m_AddIconButton = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveIconButton = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();
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
            m_MainEventLabelInputField.interactable = interactable;
            m_MainEventCodesInputField.interactable = interactable;
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