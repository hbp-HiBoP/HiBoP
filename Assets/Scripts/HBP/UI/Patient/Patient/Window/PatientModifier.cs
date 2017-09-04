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
        //[SerializeField] TransformationGestion m_TransformationGestion;
        [SerializeField] ConnectivityGestion m_ConnectivityGestion;
        [SerializeField] OthersGestion m_OthersGestion;

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
            //m_TransformationGestion.Save();
            m_ConnectivityGestion.Save();
            m_OthersGestion.Save();
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
            //m_TransformationGestion.Set(objectToDisplay);
            m_ConnectivityGestion.Set(objectToDisplay);
            m_OthersGestion.Set(objectToDisplay);
        }

        protected override void SetWindow()
        {
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_PlaceInputField.interactable = interactable;
            m_DateInputField.interactable = interactable;

            m_MeshGestion.interactable = interactable;
            m_MRIGestion.interactable = interactable;
            m_ImplantationGestion.interactable = interactable;
            //m_TransformationGestion.interactable = interactable;
            m_ConnectivityGestion.interactable = interactable;
            m_OthersGestion.interactable =interactable;

            m_SaveButton.interactable = interactable;
        }
        #endregion
    }
}