using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
	/// <summary>
	/// Display/Modify group.
	/// </summary>
	public class GroupModifier : ObjectModifier<Data.Group> 
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
                m_PatientListGestion.Interactable = false;
            }
        }
        #endregion

        #region Public Methods
        public virtual void OpenPatientsSelector()
        {
            ObjectSelector<Data.Patient> selector = ApplicationState.WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Patients.Where(p => !ItemTemp.Patients.Contains(p)));
            selector.OnSave.AddListener(() => m_PatientListGestion.List.Add(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_PatientListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        protected override void SetFields(Data.Group objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PatientListGestion.List.Set(objectToDisplay.Patients);
            m_PatientListGestion.List.OnAddObject.AddListener(patient => itemTemp.AddPatient(patient));
            m_PatientListGestion.List.OnRemoveObject.AddListener(patient => itemTemp.RemovePatient(patient));
        }
        #endregion
    }
}