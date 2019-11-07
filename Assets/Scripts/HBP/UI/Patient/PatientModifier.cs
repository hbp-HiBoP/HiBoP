using UnityEngine.UI;
using UnityEngine;
using System.Linq;

namespace HBP.UI
{
    public class PatientModifier : ObjectModifier<Data.Patient>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PlaceInputField;
        [SerializeField] InputField m_DateInputField;

        [SerializeField] MeshListGestion m_MeshListGestion;
        [SerializeField] MRIListGestion m_MRIListGestion;
        [SerializeField] SiteListGestion m_SiteListGestion;
        [SerializeField] TagValueListGestion m_TagValueListGestion;

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

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_PlaceInputField.onValueChanged.AddListener((value) => ItemTemp.Place = value);
            m_DateInputField.onValueChanged.AddListener((value) => ItemTemp.Date = int.Parse(value));

            m_MeshListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
            m_SiteListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
            m_MRIListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
            m_TagValueListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        protected override void SetFields(Data.Patient objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PlaceInputField.text = objectToDisplay.Place;
            m_DateInputField.text = objectToDisplay.Date.ToString();

            m_MeshListGestion.List.Set(objectToDisplay.Meshes);
            m_MeshListGestion.List.OnAddObject.AddListener(mesh => ItemTemp.Meshes.Add(mesh));
            m_MeshListGestion.List.OnRemoveObject.AddListener(mesh => ItemTemp.Meshes.Remove(mesh));
            m_MeshListGestion.List.OnUpdateObject.AddListener(mesh => { ItemTemp.Meshes[ItemTemp.Meshes.FindIndex(m => m.Equals(mesh))] = mesh; });

            m_MRIListGestion.List.Set(objectToDisplay.MRIs);
            m_MRIListGestion.List.OnAddObject.AddListener(mri => ItemTemp.MRIs.Add(mri));
            m_MRIListGestion.List.OnRemoveObject.AddListener(mri => ItemTemp.MRIs.Remove(mri));
            m_MRIListGestion.List.OnUpdateObject.AddListener(mri => { ItemTemp.MRIs[ItemTemp.Meshes.FindIndex(m => m.Equals(mri))] = mri; });

            m_SiteListGestion.List.Set(objectToDisplay.Sites);
            m_SiteListGestion.List.OnAddObject.AddListener(site => ItemTemp.Sites.Add(site));
            m_SiteListGestion.List.OnRemoveObject.AddListener(site => ItemTemp.Sites.Remove(site));
            m_SiteListGestion.List.OnUpdateObject.AddListener(site => { ItemTemp.Sites[ItemTemp.Sites.FindIndex(s => s.Equals(site))] = site; });

            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Preferences.PatientsTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
            m_TagValueListGestion.List.OnAddObject.AddListener(tag => ItemTemp.Tags.Add(tag));
            m_TagValueListGestion.List.OnRemoveObject.AddListener(tag => ItemTemp.Tags.Remove(tag));
            m_TagValueListGestion.List.OnUpdateObject.AddListener(tag => { ItemTemp.Tags[ItemTemp.Tags.FindIndex(t => t.Equals(tag))] = tag; });
        }
        #endregion
    }
}