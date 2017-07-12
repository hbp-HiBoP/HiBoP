using UnityEngine.UI;
using UnityEngine;


namespace HBP.UI.Anatomy
{
    public class PatientModifier : ItemModifier<Data.Patient>
    {
        #region Properties
        // General.
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PlaceInputField;
        [SerializeField] InputField m_DateInputField;

        // Meshes.
        [SerializeField] MeshGestion m_MeshGestion;
        [SerializeField] MRIGestion m_MRIGestion;
        [SerializeField] ImplantationGestion m_ImplantationGestion;

        [SerializeField] Button m_SaveButton;
        #endregion

        #region Public Methods
        #endregion

        #region Protected Methods
        public override void Save()
        {
            m_MeshGestion.Save();
            m_MRIGestion.Save();
            m_ImplantationGestion.Save();
            base.Save();
        }
        protected override void SetFields(Data.Patient objectToDisplay)
        {
            // General.
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);

            m_PlaceInputField.text = objectToDisplay.Place;
            m_PlaceInputField.onValueChanged.RemoveAllListeners();
            m_PlaceInputField.onValueChanged.AddListener((value) => ItemTemp.Place = value);

            m_DateInputField.text = objectToDisplay.Date.ToString();
            m_DateInputField.onValueChanged.RemoveAllListeners();
            m_DateInputField.onValueChanged.AddListener((value) => ItemTemp.Date = int.Parse(value));

            m_MeshGestion.Set(objectToDisplay);
            m_MeshGestion.SetActive(true);
            m_MRIGestion.Set(objectToDisplay);
            m_ImplantationGestion.Set(objectToDisplay);

            //List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            //foreach (int value in Enum.GetValues(typeof(Epilepsy.EpilepsyType)))
            //{
            //    options.Add(new Dropdown.OptionData(Epilepsy.GetFullEpilepsyName((Epilepsy.EpilepsyType) value)));
            //}
            //m_EpilepsyDropdown.options = options;
            //m_EpilepsyDropdown.value = (int) ItemTemp.Brain.Epilepsy.Type;
            //m_EpilepsyDropdown.onValueChanged.RemoveAllListeners();
            //m_EpilepsyDropdown.onValueChanged.AddListener((value) => ItemTemp.Brain.Epilepsy.Type = (Epilepsy.EpilepsyType) value);
        }

        protected override void SetWindow()
        {
        }
        protected override void SetInteractableFields(bool interactable)
        {
            // InputField.
            m_NameInputField.interactable = interactable;
            m_PlaceInputField.interactable = interactable;
            m_DateInputField.interactable = interactable;

            // InputFile.
            //m_EpilepsyDropdown.interactable = interactable;

            // Meshes
            m_MeshGestion.SetInteractable(interactable);
            m_MRIGestion.SetInteractable(interactable);
            m_ImplantationGestion.SetInteractable(interactable);

            // Buttons.
            m_SaveButton.interactable = interactable;
        }
        #endregion
    }
}