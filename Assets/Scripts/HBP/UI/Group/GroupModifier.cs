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
                m_PatientListGestion.Interactable = value;
                m_PatientListGestion.Modifiable = false;
            }
        }
        #endregion

        #region Public Methods
        public virtual void OpenPatientsSelector()
        {
            ObjectSelector<Data.Patient> selector = ApplicationState.WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Patients.Where(p => !ItemTemp.Patients.Contains(p)));
            selector.OnOk.AddListener(() => m_PatientListGestion.List.Add(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_PatientListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_PatientListGestion.List.OnAddObject.AddListener(OnAddPatient);
            m_PatientListGestion.List.OnRemoveObject.AddListener(OnRemovePatient);
        }
        protected override void SetFields(Data.Group objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PatientListGestion.List.Set(objectToDisplay.Patients);
        }

        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        protected void OnAddPatient(Data.Patient patient)
        {
            ItemTemp.AddPatient(patient);
        }
        protected void OnRemovePatient(Data.Patient patient)
        {
            ItemTemp.RemovePatient(patient);
        }
        #endregion
    }
}