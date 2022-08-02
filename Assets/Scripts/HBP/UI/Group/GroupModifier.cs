using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
	/// <summary>
	/// Window to modify a group.
	/// </summary>
	public class GroupModifier : ObjectModifier<Core.Data.Group> 
	{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] PatientListGestion m_PatientListGestion;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Open a patient selector.
        /// </summary>
        public virtual void OpenPatientsSelector()
        {
            ObjectSelector<Core.Data.Patient> selector = ApplicationState.WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Patients.Where(p => !ObjectTemp.Patients.Contains(p)));
            selector.OnOk.AddListener(() => m_PatientListGestion.List.Add(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_PatientListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_PatientListGestion.List.OnAddObject.AddListener(AddPatient);
            m_PatientListGestion.List.OnRemoveObject.AddListener(RemovePatient);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Group to modify</param>
        protected override void SetFields(Core.Data.Group objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PatientListGestion.List.Set(objectToDisplay.Patients);
        }
        /// <summary>
        /// Change the name of the group.
        /// </summary>
        /// <param name="name">Name</param>
        protected void ChangeName(string name)
        {
            if(name != "")
            {
                ObjectTemp.Name = name;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Add patient to the group.
        /// </summary>
        /// <param name="patient">Patient to add</param>
        protected void AddPatient(Core.Data.Patient patient)
        {
            ObjectTemp.Patients.Add(patient);
        }
        /// <summary>
        /// Remove patient from the group.
        /// </summary>
        /// <param name="patient">Patient to remove</param>
        protected void RemovePatient(Core.Data.Patient patient)
        {
            ObjectTemp.Patients.Remove(patient);
        }
        #endregion
    }
}