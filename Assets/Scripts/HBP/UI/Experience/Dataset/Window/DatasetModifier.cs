using UnityEngine.UI;
using System.Linq;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
	/// <summary>
	/// Display/Modify dataset.
	/// </summary>
	public class DatasetModifier : ItemModifier<d.Dataset> 
	{
		#region Properties		
		DataInfoList dataInfoList;
		InputField nameInputField;
        Button saveButton, createButton, removeButton;
        #endregion

        #region Public Methods
		public override void Save()
        {
            dataInfoList.SaveAll();
            base.Save();
		}			
		public void Create()
		{
            d.DataInfo newDataInfo = new d.DataInfo();
			ItemTemp.Data.Add(newDataInfo);
            dataInfoList.Add(newDataInfo);
		}
		public void Remove()
		{
            d.DataInfo[] dataInfoToRemove = dataInfoList.GetObjectsSelected();
            ItemTemp.Data.Remove(dataInfoToRemove);
            dataInfoList.Remove(dataInfoToRemove);
		}
        #endregion

        #region Protected Methods
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            nameInputField.text = ItemTemp.Name;
            nameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            dataInfoList.Display(ItemTemp.Data.ToArray());           
        }
        protected override void SetWindow()
        {
            nameInputField = transform.FindChild("Content").FindChild("Name").FindChild("InputField").GetComponent<InputField>();
            dataInfoList = transform.FindChild("Content").FindChild("Data").FindChild("List").FindChild("List").FindChild("List").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<DataInfoList>();
            saveButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Save").GetComponent<Button>();
            createButton = transform.FindChild("Content").FindChild("Data").FindChild("List").FindChild("Buttons").FindChild("Add").GetComponent<Button>();
            removeButton = transform.FindChild("Content").FindChild("Data").FindChild("List").FindChild("Buttons").FindChild("Remove").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            nameInputField.interactable = interactable;
            saveButton.interactable = interactable;
            createButton.interactable = interactable;
            removeButton.interactable = interactable;
        }
        #endregion
    }
}