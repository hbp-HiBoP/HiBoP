using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using d = HBP.Data.Experience.Protocol;
using System.Collections.Generic;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolModifier : ItemModifier<d.Protocol> 
	{
        #region Properties
        [SerializeField] GameObject blocModifierPrefab;
        List<BlocModifier> m_Modifiers = new List<BlocModifier>();

        InputField nameInputField;
		BlocGrid blocGrid;
        Button saveButton;
        #endregion

        #region Public Methods
        public override void Close()
        {
            foreach (var modifier in m_Modifiers.ToArray()) modifier.Close();
            m_Modifiers.Clear();
            base.Close();
        }
        #endregion

        #region Private Methods
        protected void OnListEvent(d.Bloc bloc, int type)
        {
            ItemTemp.Blocs = blocGrid.Objects.ToList();
            if (type == 0 || type == -1) OpenBlocModifier(bloc);
        }
        protected void OpenBlocModifier(d.Bloc bloc)
        {
            if(bloc.MainEvent == null) bloc.Events.Add(new d.Event("Main", new int[0], d.Event.TypeEnum.Main));
            RectTransform obj = Instantiate(blocModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            BlocModifier modifier = obj.GetComponent<BlocModifier>();
            modifier.Open(bloc, true);
            modifier.CloseEvent.AddListener(() => OnCloseBlocModifier(modifier));
            modifier.SaveEvent.AddListener(() => OnSaveBlocModifier(modifier));
            m_Modifiers.Add(modifier);
        }
        protected void OnSaveBlocModifier(BlocModifier modifier)
        {
            if(!ItemTemp.Blocs.Contains(modifier.Item))
            {
                ItemTemp.Blocs.Add(modifier.Item);
            }
            blocGrid.Display(ItemTemp.Blocs.ToArray());
        }
        protected void OnCloseBlocModifier(BlocModifier modifier)
        {
            m_Modifiers.Remove(modifier);
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
            blocGrid = transform.Find("Content").Find("Blocs").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<BlocGrid>();
            saveButton  = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
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
