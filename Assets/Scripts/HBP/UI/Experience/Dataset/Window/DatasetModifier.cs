using UnityEngine;
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
        [SerializeField] GameObject m_DataInfoModifierPrefab;
        #endregion

        #region Public Methods			
		public void Add()
		{
            OpenDataInfoModifier(new d.DataInfo());
		}
		public void Remove()
		{
            d.DataInfo[] dataInfoToRemove = m_DataInfoList.ObjectsSelected;
            ItemTemp.Data.Remove(dataInfoToRemove);
            m_DataInfoList.Remove(dataInfoToRemove);
		}
        #endregion

        #region Protected Methods
        protected void OpenDataInfoModifier(d.DataInfo dataInfo)
        {
            RectTransform obj = Instantiate(m_DataInfoModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            DataInfoModifier dataInfoModifier = obj.GetComponent<DataInfoModifier>();
            dataInfoModifier.Open(dataInfo, true);
            dataInfoModifier.SaveEvent.AddListener(() => OnSaveEventModifier(dataInfoModifier));
        }
        protected void OnSaveEventModifier(DataInfoModifier eventModifier)
        {
            // Save
            if (!ItemTemp.Data.Contains(eventModifier.Item))
            {
                ItemTemp.Data.Add(eventModifier.Item);
                m_DataInfoList.Add(eventModifier.Item);
            }
            else
            {
                m_DataInfoList.UpdateObject(eventModifier.Item);
            }
        }
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            m_NameInputField.text = ItemTemp.Name;
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_DataInfoList.Objects = ItemTemp.Data.ToArray();
            m_DataInfoList.SortByName(DataInfoList.Sorting.Descending);
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