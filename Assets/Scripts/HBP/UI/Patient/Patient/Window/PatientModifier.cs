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
        [SerializeField] ConnectivityGestion m_ConnectivityGestion;
        [SerializeField] OthersGestion m_OthersGestion;
        #endregion

        #region Public Methods
        public override void Close()
        {
            m_MeshGestion.Close();
            m_MRIGestion.Close();
            m_ImplantationGestion.Close();
            m_ConnectivityGestion.Close();
            base.Close();
        }
        public override void Save()
        {
            m_MeshGestion.Save();
            m_MRIGestion.Save();
            m_ImplantationGestion.Save();
            m_ConnectivityGestion.Save();
            m_OthersGestion.Save();
            base.Save();
        }
        #endregion

        #region Protected Methods
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

            m_MeshGestion.Set(ItemTemp);
            m_MeshGestion.SetActive(true);
            m_MRIGestion.Set(objectToDisplay);
            m_ImplantationGestion.Set(objectToDisplay);
            m_ConnectivityGestion.Set(objectToDisplay);
            m_OthersGestion.Set(objectToDisplay);
        }
        protected override void SetInteractable(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_PlaceInputField.interactable = interactable;
            m_DateInputField.interactable = interactable;

            m_MeshGestion.interactable = interactable;
            m_MRIGestion.interactable = interactable;
            m_ImplantationGestion.interactable = interactable;
            m_ConnectivityGestion.interactable = interactable;
            m_OthersGestion.interactable = interactable;
        }
        #endregion
    }
}