using UnityEngine.UI;
   using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
	/// <summary>
	/// Display/Modify dataset.
	/// </summary>
	public class DatasetModifier : ItemModifier<d.Dataset> 
	{
		#region Properties		
		DataInfoList m_DataInfoList;
		InputField m_NameInputField;
        Button m_SaveButton, m_CreateButton, m_RemoveButton;
        #endregion

        #region Public Methods
		public override void Save()
        {
            m_DataInfoList.SaveAll();
            base.Save();
		}			
		public void Create()
		{
            d.DataInfo newDataInfo = new d.DataInfo();
			ItemTemp.Data.Add(newDataInfo);
            m_DataInfoList.Add(newDataInfo);
		}
		public void Remove()
		{
            d.DataInfo[] dataInfoToRemove = m_DataInfoList.ObjectsSelected;
            ItemTemp.Data.Remove(dataInfoToRemove);
            m_DataInfoList.Remove(dataInfoToRemove);
		}
        #endregion

        #region Protected Methods
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            m_NameInputField.text = ItemTemp.Name;
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_DataInfoList.Objects = ItemTemp.Data.ToArray();           
        }
        protected override void SetWindow()
        {
            m_NameInputField = transform.Find("Content").Find("Name").Find("InputField").GetComponent<InputField>();
            m_DataInfoList = transform.Find("Content").Find("DataInfo").Find("List").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<DataInfoList>();
            m_SaveButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
            m_CreateButton = transform.Find("Content").Find("DataInfo").Find("List").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveButton = transform.Find("Content").Find("DataInfo").Find("List").Find("Buttons").Find("Remove").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_SaveButton.interactable = interactable;
            m_CreateButton.interactable = interactable;
            m_RemoveButton.interactable = interactable;
            m_DataInfoList.interactable = interactable;
        }
        #endregion
    }
}