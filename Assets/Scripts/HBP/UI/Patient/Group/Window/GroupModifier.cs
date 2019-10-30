using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Display/Modify group.
	/// </summary>
	public class GroupModifier : ItemModifier<Group> 
	{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] PatientListGestion m_PatientListGestion;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_PatientListGestion.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public virtual void OpenPatientsSelector()
        {
            ObjectSelector<Patient> selector = ApplicationState.WindowsManager.OpenSelector<Patient>();
            selector.OnSave.AddListener(() => OnSaveSelector(selector));
            selector.Objects = ApplicationState.ProjectLoaded.Patients.ToArray();
            selector.MultiSelection = true;
            selector.OpenModifierWhenSave = false;
            SubWindowsManager.Add(selector);
        }
        protected virtual void OnSaveSelector(ObjectSelector<Patient> selector)
        {
            m_PatientListGestion.List.Add(selector.ObjectsSelected);
            SubWindowsManager.Remove(selector);
        }
        public override void Save()
        {
            itemTemp.SetPatients(m_PatientListGestion.List.Objects);
            base.Save();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);

            m_PatientListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
        }
        protected override void SetFields(Group objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PatientListGestion.List.Set(objectToDisplay.Patients);
        }
        #endregion
    }
}