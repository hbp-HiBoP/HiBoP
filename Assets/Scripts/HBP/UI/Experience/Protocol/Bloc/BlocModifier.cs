using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ItemModifier<d.Bloc> 
	{
		#region Properties
		InputField nameInputField;
		FileSelector imageFileSelector;
		InputField sortInputField;
		InputField windowMinInputField;
		InputField windowMaxInputField;
        InputField baseLineMinInputField;
        InputField baseLineMaxInputField;
		InputField mainEventLabelInputField;
		InputField mainEventCodesInputField;
		EventList eventList;
        IconList iconList;
		Button saveButton;
		Button addEventButton;
		Button removeEventButton;
        Button addIconButton;
        Button removeIconButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            iconList.SaveAll();
            eventList.SaveAll();
            base.Save();
        }
        public void AddSecondaryEvent()
		{
            d.Event newEvent = new d.Event();
            ItemTemp.SecondaryEvents.Add(newEvent);
            eventList.Add(newEvent);
        }
        public void RemoveSecondaryEvent()
		{
            d.Event[] eventsToRemove = eventList.ObjectsSelected;
            foreach(d.Event e in eventsToRemove) ItemTemp.SecondaryEvents.Remove(e);
            eventList.Remove(eventsToRemove);
        }
        public void AddIcon()
        {
            d.Icon newIcon = new d.Icon();
            ItemTemp.Scenario.Icons.Add(newIcon);
            iconList.Add(newIcon);
        }
        public void RemoveIcon()
        {
            d.Icon[] iconsToRemove = iconList.ObjectsSelected;
            foreach(d.Icon i in iconsToRemove) ItemTemp.Scenario.Icons.Remove(i);
            iconList.Remove(iconsToRemove);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            nameInputField.text = objectToDisplay.DisplayInformations.Name;
            nameInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Name = value);

            imageFileSelector.File = objectToDisplay.DisplayInformations.IllustrationPath;
            imageFileSelector.onValueChanged.AddListener((value) => ItemTemp.DisplayInformations.IllustrationPath = value);

            sortInputField.text = objectToDisplay.DisplayInformations.Sort;
            sortInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Sort = value);

            windowMinInputField.text = objectToDisplay.DisplayInformations.Window.Start.ToString();
            windowMinInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Window = new Tools.CSharp.Window(float.Parse(value), ItemTemp.DisplayInformations.Window.End));

            windowMaxInputField.text = objectToDisplay.DisplayInformations.Window.End.ToString();
            windowMaxInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.Window = new Tools.CSharp.Window(ItemTemp.DisplayInformations.Window.Start, float.Parse(value)));

            baseLineMinInputField.text = objectToDisplay.DisplayInformations.BaseLine.Start.ToString();
            baseLineMinInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.BaseLine = new Tools.CSharp.Window(float.Parse(value),ItemTemp.DisplayInformations.BaseLine.End));

            baseLineMaxInputField.text = objectToDisplay.DisplayInformations.BaseLine.End.ToString();
            baseLineMaxInputField.onEndEdit.AddListener((value) => ItemTemp.DisplayInformations.BaseLine = new Tools.CSharp.Window(ItemTemp.DisplayInformations.BaseLine.Start, float.Parse(value)));

            mainEventLabelInputField.text = objectToDisplay.MainEvent.Name;
            mainEventLabelInputField.onEndEdit.AddListener((value) => ItemTemp.MainEvent.Name = value);

            mainEventCodesInputField.text = objectToDisplay.MainEvent.CodesString;
            mainEventCodesInputField.onEndEdit.AddListener((value) => ItemTemp.MainEvent.CodesString = value);

            eventList.Objects = ItemTemp.SecondaryEvents.ToArray();
            iconList.Objects = ItemTemp.Scenario.Icons.ToArray();
        }
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").Find("Display informations").Find("Name").Find("InputField").GetComponent<InputField>();
            imageFileSelector = transform.Find("Content").Find("Display informations").Find("Image").Find("Illustration").Find("FileSelector").GetComponent<FileSelector>();
            sortInputField = transform.Find("Content").Find("Display informations").Find("Sort").Find("InputField").GetComponent<InputField>();
            windowMinInputField = transform.Find("Content").Find("Display informations").Find("Window").Find("Panel").Find("Min").Find("InputField").GetComponent<InputField>();
            windowMaxInputField = transform.Find("Content").Find("Display informations").Find("Window").Find("Panel").Find("Max").Find("InputField").GetComponent<InputField>();
            baseLineMinInputField = transform.Find("Content").Find("Display informations").Find("BaseLine").Find("Panel").Find("Min").Find("InputField").GetComponent<InputField>();
            baseLineMaxInputField = transform.Find("Content").Find("Display informations").Find("BaseLine").Find("Panel").Find("Max").Find("InputField").GetComponent<InputField>();
            mainEventLabelInputField = transform.Find("Content").Find("Main Event").Find("Label").Find("InputField").GetComponent<InputField>();
            mainEventCodesInputField = transform.Find("Content").Find("Main Event").Find("Code").Find("InputField").GetComponent<InputField>();
            eventList = transform.Find("Content").Find("Secondary Events").Find("List").Find("List").Find("Viewport").Find("Content").GetComponent<EventList>();
            iconList = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("List").Find("Viewport").Find("Content").GetComponent<IconList>();
            saveButton = transform.Find("Content").Find("Buttons").Find("Save").GetComponent<Button>();
            addEventButton = transform.Find("Content").Find("Secondary Events").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            removeEventButton = transform.Find("Content").Find("Secondary Events").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();
            addIconButton = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            removeIconButton = transform.Find("Content").Find("Iconic Scenario").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            nameInputField.interactable = interactable;
            imageFileSelector.interactable = interactable;
            sortInputField.interactable = interactable;
            windowMinInputField.interactable = interactable;
            windowMaxInputField.interactable = interactable;
            baseLineMinInputField.interactable = interactable;
            baseLineMaxInputField.interactable = interactable;
            mainEventLabelInputField.interactable = interactable;
            mainEventCodesInputField.interactable = interactable;
            saveButton.interactable = interactable;
            addEventButton.interactable = interactable;
            removeEventButton.interactable = interactable;
            addIconButton.interactable = interactable;
            removeIconButton.interactable = interactable;
        }
        #endregion
    }
}