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
                m_PlaceInputField.interactable = value;
                m_DateInputField.interactable = value;

                m_MeshGestion.interactable = value;
                m_MRIGestion.interactable = value;
                m_ImplantationGestion.interactable = value;
                m_ConnectivityGestion.interactable = value;
                m_OthersGestion.interactable = value;

                m_SaveButton.interactable = value;
            }
        }
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
        protected override void Initialize()
        {
            base.Initialize();
            m_MeshGestion.Initialize();
            m_MRIGestion.Initialize();
            m_ImplantationGestion.Initialize();
            m_ConnectivityGestion.Initialize();
            m_OthersGestion.Initialize();
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

            m_MeshGestion.Set(ItemTemp);
            m_MeshGestion.SetActive(true);
            m_MRIGestion.Set(objectToDisplay);
            m_ImplantationGestion.Set(objectToDisplay);
            m_ConnectivityGestion.Set(objectToDisplay);
            m_OthersGestion.Set(objectToDisplay);
        }
        #endregion
    }
}