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
        List<ItemModifier<d.Bloc>> m_Modifiers = new List<ItemModifier<d.Bloc>>();

        [SerializeField] InputField m_NameInputField;
        [SerializeField] BlocList m_BlocList;
        [SerializeField] Button m_AddBlocButton, m_RemoveBlocButton;
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
            //ItemTemp.Blocs = blocGrid.Objects.ToList();
            if (type == 0 || type == -1) OpenBlocModifier(bloc);
        }
        protected void OpenBlocModifier(d.Bloc bloc)
        {
            //if(bloc.MainEvent == null) bloc.Events.Add(new d.Event("Main", new int[0], d.Event.TypeEnum.Main));
            ItemModifier<d.Bloc> modifier = ApplicationState.WindowsManager.OpenModifier(bloc, true);
            modifier.OnClose.AddListener(() => OnCloseBlocModifier(modifier));
            modifier.OnSave.AddListener(() => OnSaveBlocModifier(modifier));
            m_Modifiers.Add(modifier);
        }
        protected void OnSaveBlocModifier(ItemModifier<d.Bloc> modifier)
        {
            if(!ItemTemp.Blocs.Contains(modifier.Item))
            {
                ItemTemp.Blocs.Add(modifier.Item);
            }
            //blocGrid.Display(ItemTemp.Blocs.ToArray());
        }
        protected void OnCloseBlocModifier(ItemModifier<d.Bloc> modifier)
        {
            m_Modifiers.Remove(modifier);
        }
        protected override void SetFields(d.Protocol objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            //blocGrid.Display(objectToDisplay.Blocs.ToArray());
            //blocGrid.OnAction.AddListener((bloc, i) => OnListEvent(bloc, i));
        }
        protected override void Initialize()
        {
        }
        protected override void SetInteractable(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_SaveButton.interactable = interactable;
            m_AddBlocButton.interactable = interactable;
            m_RemoveBlocButton.interactable = interactable;
        }
        #endregion
    }
}
