using UnityEngine;
using UnityEngine.UI;
using System.Linq;
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
        [SerializeField] Button m_SaveButton;

        // Timeline
        [SerializeField] InputField m_StartWindowInputField;
        [SerializeField] InputField m_EndWindowInputField;

        [SerializeField] InputField m_StartBaselineInputField;
        [SerializeField] InputField m_EndBaselineInputField;

        // Events
        [SerializeField] GameObject m_EventModifierPrefab;
        List<EventModifier> m_EventModifiers = new List<EventModifier>();
        [SerializeField] EventList m_EventList;
        [SerializeField] Button m_AddEventButton, m_RemoveEventButton;
        // Icons
        [SerializeField] GameObject m_IconModifierPrefab;
        List<IconModifier> m_IconModifiers = new List<IconModifier>();
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
            foreach (var modifier in m_TreatmentModifiers) modifier.Close();
            base.Close();
        }
        public override void Save()
        {
            foreach (var modifier in m_EventModifiers) modifier.Save();
            foreach (var modifier in m_IconModifiers) modifier.Save();
            foreach (var modifier in m_TreatmentModifiers) modifier.Save();
            base.Save();
        }
        #endregion

        #region Private Methods
        // Events
        protected void OpenEventModifier(d.Event eventToModify)
        {
            GameObject go = Instantiate(m_EventModifierPrefab, GameObject.Find("Windows").transform);
            go.transform.localPosition = Vector3.zero;
            EventModifier modifier = go.GetComponent<EventModifier>();
            modifier.Open(eventToModify, interactable);
        }
        protected void OnSaveEventModifier(EventModifier modifier)
        {
            if (!ItemTemp.Events.Contains(modifier.Item)) ItemTemp.Events.Add(modifier.Item);
            m_EventList.Objects = ItemTemp.Events.ToArray();
        }
        protected void OnCloseEventModifier(EventModifier modifier)
        {
            m_EventModifiers.Remove(modifier);
        }

        // Icons
        protected void OpenIconModifier(d.Icon iconToModify)
        {

        }
        protected void OnSaveIconModifier(IconModifier modifier)
        {
            if (!ItemTemp.Events.Contains(modifier.Item)) ItemTemp.Events.Add(modifier.Item);
            m_EventList.Objects = ItemTemp.Events.ToArray();
        }
        protected void OnCloseIconModifier(IconModifier modifier)
        {
            m_EventModifiers.Remove(modifier);
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
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_SaveButton.interactable = interactable;
            m_AddEventButton.interactable = interactable;
            m_RemoveBlocButton.interactable = interactable;
        }
        #endregion
    }
}