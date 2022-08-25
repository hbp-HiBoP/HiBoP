using HBP.Core.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a patient.
    /// </summary>
    public class PatientModifier : ObjectModifier<Patient>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PlaceInputField;
        [SerializeField] InputField m_DateInputField;

        [SerializeField] MeshListGestion m_MeshListGestion;
        [SerializeField] MRIListGestion m_MRIListGestion;
        [SerializeField] SiteListGestion m_SiteListGestion;
        [SerializeField] TagValueListGestion m_TagValueListGestion;

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
                m_PlaceInputField.interactable = value;
                m_DateInputField.interactable = value;

                m_MeshListGestion.Interactable = value;
                m_MeshListGestion.Modifiable = value;

                m_MRIListGestion.Interactable = value;
                m_MRIListGestion.Modifiable = value;

                m_SiteListGestion.Interactable = value;
                m_SiteListGestion.Modifiable = value;

                m_TagValueListGestion.Interactable = value;
                m_TagValueListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_PlaceInputField.onEndEdit.AddListener(ChangePlace);
            m_DateInputField.onEndEdit.AddListener(ChangeDate);

            m_MeshListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_MeshListGestion.List.OnAddObject.AddListener(AddMesh);
            m_MeshListGestion.List.OnRemoveObject.AddListener(RemoveMesh);
            m_MeshListGestion.List.OnUpdateObject.AddListener(UpdateMesh);

            m_MRIListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_MRIListGestion.List.OnAddObject.AddListener(AddMRI);
            m_MRIListGestion.List.OnRemoveObject.AddListener(RemoveMRI);
            m_MRIListGestion.List.OnUpdateObject.AddListener(UpdateMRI);

            m_SiteListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_SiteListGestion.List.OnAddObject.AddListener(AddSite);
            m_SiteListGestion.List.OnRemoveObject.AddListener(RemoveSite);
            m_SiteListGestion.List.OnUpdateObject.AddListener(UpdateSite);

            m_TagValueListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_TagValueListGestion.List.OnAddObject.AddListener(AddTag);
            m_TagValueListGestion.List.OnRemoveObject.AddListener(RemoveTag);
            m_TagValueListGestion.List.OnUpdateObject.AddListener(UpdateTag);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Patient to display</param>
        protected override void SetFields(Patient objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PlaceInputField.text = objectToDisplay.Place;
            m_DateInputField.text = objectToDisplay.Date.ToString();

            m_MeshListGestion.List.Set(objectToDisplay.Meshes);
            m_MRIListGestion.List.Set(objectToDisplay.MRIs);
            m_SiteListGestion.List.Set(objectToDisplay.Sites);
            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Preferences.PatientsTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
        }
        /// <summary>
        /// Change the patient name.
        /// </summary>
        /// <param name="name">Name of the patient</param>
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
        /// Change the patient place.
        /// </summary>
        /// <param name="place">Patient place</param>
        protected void ChangePlace(string place)
        {
            if(place != "")
            {
                ObjectTemp.Place = place;
            }
            else
            {
                m_PlaceInputField.text = ObjectTemp.Place;
            }
        }
        /// <summary>
        /// Change the patient date.
        /// </summary>
        /// <param name="date">Patient date</param>
        protected void ChangeDate(string value)
        {
            if(int.TryParse(value, out int date))
            {
                ObjectTemp.Date = date;
            }
            else
            {
                m_DateInputField.text = ObjectTemp.Date.ToString();
            }
        }

        /// <summary>
        /// Add mesh to the patient.
        /// </summary>
        /// <param name="mesh">Mesh to add</param>
        protected void AddMesh(BaseMesh mesh)
        {
            if(!ObjectTemp.Meshes.Contains(mesh))
            {
                ObjectTemp.Meshes.Add(mesh);
            }
        }
        /// <summary>
        /// Remove mesh from the patient.
        /// </summary>
        /// <param name="mesh">Mesh to remove</param>
        protected void RemoveMesh(BaseMesh mesh)
        {
            if(ObjectTemp.Meshes.Contains(mesh))
            {
                ObjectTemp.Meshes.Remove(mesh);
            }
        }
        /// <summary>
        /// Update mesh from the patient.
        /// </summary>
        /// <param name="mesh">Mesh to update</param>
        protected void UpdateMesh(BaseMesh mesh)
        {
            int index = ObjectTemp.Meshes.FindIndex(m => m.Equals(mesh));
            if(index != -1)
            {
                ObjectTemp.Meshes[index] = mesh;
            }
        }

        /// <summary>
        /// Add MRI to the patient.
        /// </summary>
        /// <param name="mri">MRI to add</param>
        protected void AddMRI(MRI mri)
        {
            if (!ObjectTemp.MRIs.Contains(mri))
            {
                ObjectTemp.MRIs.Add(mri);
            }
        }
        /// <summary>
        /// Remove MRI from the patient.
        /// </summary>
        /// <param name="mri">MRI to remove</param>
        protected void RemoveMRI(MRI mri)
        {
            if (ObjectTemp.MRIs.Contains(mri))
            {
                ObjectTemp.MRIs.Remove(mri);
            }
        }
        /// <summary>
        /// Update MRI from the patient.
        /// </summary>
        /// <param name="mri">MRI to update</param>
        protected void UpdateMRI(MRI mri)
        {
            int index = ObjectTemp.MRIs.FindIndex(m => m.Equals(mri));
            if (index != -1)
            {
                ObjectTemp.MRIs[index] = mri;
            }
        }

        /// <summary>
        /// Add site to the patient.
        /// </summary>
        /// <param name="site">site to add</param>
        protected void AddSite(Site site)
        {
            if (!ObjectTemp.Sites.Contains(site))
            {
                ObjectTemp.Sites.Add(site);
            }
        }
        /// <summary>
        /// Remove site from the patient.
        /// </summary>
        /// <param name="site">site to remove</param>
        protected void RemoveSite(Site site)
        {
            if (ObjectTemp.Sites.Contains(site))
            {
                ObjectTemp.Sites.Remove(site);
            }
        }
        /// <summary>
        /// Update site from the patient.
        /// </summary>
        /// <param name="site">Site to update</param>
        protected void UpdateSite(Site site)
        {
            int index = ObjectTemp.Sites.FindIndex(s => s.Equals(site));
            if (index != -1)
            {
                ObjectTemp.Sites[index] = site;
            }
        }

        /// <summary>
        /// Add tag to the patient.
        /// </summary>
        /// <param name="tag">Tag to add</param>
        protected void AddTag(BaseTagValue tag)
        {
            if (!ObjectTemp.Tags.Contains(tag))
            {
                ObjectTemp.Tags.Add(tag);
            }
        }
        /// <summary>
        /// Remove tag from the patient.
        /// </summary>
        /// <param name="tag">Tag to remove</param>
        protected void RemoveTag(BaseTagValue tag)
        {
            if (ObjectTemp.Tags.Contains(tag))
            {
                ObjectTemp.Tags.Remove(tag);
            }
        }
        /// <summary>
        /// Update tag from the patient.
        /// </summary>
        /// <param name="tag">Tag to update</param>
        protected void UpdateTag(BaseTagValue tag)
        {
            int index = ObjectTemp.Tags.FindIndex(t => t.Equals(tag));
            if (index != -1)
            {
                ObjectTemp.Tags[index] = tag;
            }
        }
        #endregion
    }
} 