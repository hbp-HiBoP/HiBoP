using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolModifier : ItemModifier<d.Protocol> 
	{
        #region Properties
        [SerializeField]
        GameObject blocModifierPrefab;
        BlocModifier blocModifier;

        InputField nameInputField;
		BlocGrid blocGrid;
        Button saveButton;
        #endregion

        #region Private Methods
        protected void OnListEvent(d.Bloc bloc, int type)
        {
            if (type == 0 || type == -1) OpenBlocModifier(bloc);
        }
        protected void OpenBlocModifier(d.Bloc bloc)
        {
            RectTransform obj = Instantiate(blocModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            blocModifier = obj.GetComponent<BlocModifier>();
            blocModifier.Open(bloc, true);
            blocModifier.CloseEvent.AddListener(() => OnCloseBlocModifier());
            blocModifier.SaveEvent.AddListener(() => OnSaveBlocModifier());
            SetInteractable(false);
        }
        protected void OnSaveBlocModifier()
        {
            if(!ItemTemp.Blocs.Contains(blocModifier.Item))
            {
                ItemTemp.Blocs.Add(blocModifier.Item);
            }
            blocGrid.Display(ItemTemp.Blocs.ToArray());
        }
        protected void OnCloseBlocModifier()
        {
            SetInteractable(true);
        }
        protected override void SetFields(d.Protocol objectToDisplay)
        {
            nameInputField.text = objectToDisplay.Name;
            nameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            blocGrid.Display(objectToDisplay.Blocs.ToArray());
            blocGrid.OnAction.AddListener((bloc, i) => OnListEvent(bloc, i));
        }
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").Find("Name").Find("InputField").GetComponent<InputField>();
            blocGrid = transform.Find("Content").Find("Blocs").Find("List").Find("List").Find("Viewport").Find("Content").GetComponent<BlocGrid>();
            saveButton  = transform.Find("Content").Find("Buttons").Find("Save").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            nameInputField.interactable = interactable;
            saveButton.interactable = interactable;
            // TODO
            //blocGrid.interactable = interactable;
        }
        #endregion
    }
}
