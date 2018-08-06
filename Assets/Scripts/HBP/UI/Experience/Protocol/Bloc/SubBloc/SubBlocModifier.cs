using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using System.Collections.Generic;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocModifier : ItemModifier<d.SubBloc>
    {
        #region Properties
        // General
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PositionInputField;

        // Timeline
        [SerializeField] InputField m_StartWindowInputField;
        [SerializeField] InputField m_EndWindowInputField;

        [SerializeField] InputField m_StartBaselineInputField;
        [SerializeField] InputField m_EndBaselineInputField;

        // Events
        [SerializeField] GameObject m_EventModifierPrefab;
        List<ItemModifier<d.Event>> m_EventModifiers = new List<ItemModifier<d.Event>>();
        [SerializeField] EventList m_EventList;
        [SerializeField] Button m_AddEventButton, m_RemoveEventButton;
        // Icons
        [SerializeField] GameObject m_IconModifierPrefab;
        List<ItemModifier<d.Icon>> m_IconModifiers = new List<ItemModifier<d.Icon>>();
        [SerializeField] IconList m_IconList;
        [SerializeField] Button m_AddIconButton, m_RemoveIconButton;

        // Treatments
        [SerializeField] GameObject m_TreatmentModifierPrefab;
        //List<TreatmentModifier> m_TreatmentModifiers = new List<TreatmentModifier>();
        [SerializeField] Button m_AddTreatmentButton, m_RemoveTreatmentButton;

        #endregion

        #region Public Methods
        public override void Close()
        {
            foreach (var modifier in m_EventModifiers) modifier.Close();
            foreach (var modifier in m_IconModifiers) modifier.Close();
            //foreach (var modifier in m_TreatmentModifiers) modifier.Close();
            base.Close();
        }
        public override void Save()
        {
            foreach (var modifier in m_EventModifiers) modifier.Save();
            foreach (var modifier in m_IconModifiers) modifier.Save();
            //foreach (var modifier in m_TreatmentModifiers) modifier.Save();
            base.Save();
        }
        #endregion

        #region Private Methods
        // Events
        protected void OpenEventModifier(d.Event eventToModify)
        {
            ItemModifier<d.Event> modifier = ApplicationState.WindowsManager.OpenModifier(eventToModify, Interactable);
            m_EventModifiers.Add(modifier);
        }
        protected void OnSaveEventModifier(ItemModifier<d.Event> modifier)
        {
            if (!ItemTemp.Events.Contains(modifier.Item)) ItemTemp.Events.Add(modifier.Item);
            m_EventList.Objects = ItemTemp.Events.ToArray();
        }
        protected void OnCloseEventModifier(ItemModifier<d.Event> modifier)
        {
            m_EventModifiers.Remove(modifier);
        }

        // Icons
        protected void OpenIconModifier(d.Icon iconToModify)
        {

        }
        protected void OnSaveIconModifier(IconModifier modifier)
        {
            if (!ItemTemp.Icons.Contains(modifier.Item)) ItemTemp.Icons.Add(modifier.Item);
            m_IconList.Objects = ItemTemp.Icons.ToArray();
        }
        protected void OnCloseIconModifier(IconModifier modifier)
        {
            m_IconModifiers.Remove(modifier);
        }

        // Treatments
        //protected void OpenTreatmentModifier(d.Treatment treatmentToModify)
        //{

        //}
        //protected void OnSaveTreatmentModifierModifier(TreatmentModifier modifier)
        //{
        //    if (!ItemTemp.Events.Contains(modifier.Item)) ItemTemp.Events.Add(modifier.Item);
        //    m_EventList.Objects = ItemTemp.Events.ToArray();
        //}
        //protected void OnCloseTreatmentModifier(TreatmentModifier modifier)
        //{
        //    m_EventModifiers.Remove(modifier);
        //}

        protected override void SetFields(d.SubBloc objectToDisplay)
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
            m_AddEventButton.interactable = interactable;
            m_RemoveEventButton.interactable = interactable;
        }
        #endregion
    }
}