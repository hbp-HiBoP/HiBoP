using UnityEngine.UI;
using UnityEngine;
using System.Linq;

namespace HBP.UI.Anatomy
{
    public class PatientModifier : ItemModifier<Data.Patient>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PlaceInputField;
        [SerializeField] InputField m_DateInputField;

        [SerializeField] MeshListGestion m_MeshListGestion;
        [SerializeField] MRIListGestion m_MRIListGestion;
        [SerializeField] SiteListGestion m_SiteListGestion;
        [SerializeField] Tags.TagValueListGestion m_TagValueListGestion;

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

                m_MeshListGestion.Interactable = value;
                m_MRIListGestion.Interactable = value;
                m_SiteListGestion.Interactable = value;
                m_TagValueListGestion.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            SubWindowsManager.SaveAll();
            itemTemp.Meshes = m_MeshListGestion.List.Objects.ToList();
            itemTemp.MRIs = m_MRIListGestion.List.Objects.ToList();
            itemTemp.Sites = m_SiteListGestion.List.Objects.ToList();
            itemTemp.Tags = m_TagValueListGestion.List.Objects.ToList();
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_MeshListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
            m_SiteListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
            m_MRIListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
            m_TagValueListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));

            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_PlaceInputField.onValueChanged.AddListener((value) => ItemTemp.Place = value);
            m_DateInputField.onValueChanged.AddListener((value) => ItemTemp.Date = int.Parse(value));
        }
        protected override void SetFields(Data.Patient objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PlaceInputField.text = objectToDisplay.Place;
            m_DateInputField.text = objectToDisplay.Date.ToString();

            m_MeshListGestion.List.Set(objectToDisplay.Meshes);
            m_MRIListGestion.List.Set(objectToDisplay.MRIs);
            m_SiteListGestion.List.Set(objectToDisplay.Sites);
            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Settings.PatientsTags.Concat(ApplicationState.ProjectLoaded.Settings.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
        }
        #endregion
    }
}