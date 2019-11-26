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
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_PlaceInputField.onEndEdit.AddListener(OnChangePlace);
            m_DateInputField.onEndEdit.AddListener(OnChangeDate);

            m_MeshListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_MeshListGestion.List.OnAddObject.AddListener(OnAddMesh);
            m_MeshListGestion.List.OnRemoveObject.AddListener(OnRemoveMesh);
            m_MeshListGestion.List.OnUpdateObject.AddListener(OnUpdateMesh);

            m_MRIListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_MRIListGestion.List.OnAddObject.AddListener(OnAddMRI);
            m_MRIListGestion.List.OnRemoveObject.AddListener(OnRemoveMRI);
            m_MRIListGestion.List.OnUpdateObject.AddListener(OnUpdateMRI);

            m_SiteListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_SiteListGestion.List.OnAddObject.AddListener(OnAddSite);
            m_SiteListGestion.List.OnRemoveObject.AddListener(OnRemoveSite);
            m_SiteListGestion.List.OnUpdateObject.AddListener(OnUpdateSite);

            m_TagValueListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_TagValueListGestion.List.OnAddObject.AddListener(OnAddTag);
            m_TagValueListGestion.List.OnRemoveObject.AddListener(OnRemoveTag);
            m_TagValueListGestion.List.OnUpdateObject.AddListener(OnUpdateTag);
        }
        protected override void SetFields(Data.Patient objectToDisplay)
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
        protected void OnChangePlace(string value)
        {
            if(value != "")
            {
                ItemTemp.Place = value;
            }
            else
            {
                m_PlaceInputField.text = ItemTemp.Place;
            }
        }
        protected void OnChangeDate(string value)
        {
            if(int.TryParse(value, out int date))
            {
                ItemTemp.Date = date;
            }
            else
            {
                m_DateInputField.text = ItemTemp.Date.ToString();
            }
        }

        protected void OnAddMesh(Data.BaseMesh mesh)
        {
            if(!ItemTemp.Meshes.Contains(mesh))
            {
                ItemTemp.Meshes.Add(mesh);
            }
        }
        protected void OnRemoveMesh(Data.BaseMesh mesh)
        {
            if(ItemTemp.Meshes.Contains(mesh))
            {
                ItemTemp.Meshes.Remove(mesh);
            }
        }
        protected void OnUpdateMesh(Data.BaseMesh mesh)
        {
            int index = ItemTemp.Meshes.FindIndex(m => m.Equals(mesh));
            if(index != -1)
            {
                ItemTemp.Meshes[index] = mesh;
            }
        }

        protected void OnAddMRI(Data.MRI mri)
        {
            if (!ItemTemp.MRIs.Contains(mri))
            {
                ItemTemp.MRIs.Add(mri);
            }
        }
        protected void OnRemoveMRI(Data.MRI mri)
        {
            if (ItemTemp.MRIs.Contains(mri))
            {
                ItemTemp.MRIs.Remove(mri);
            }
        }
        protected void OnUpdateMRI(Data.MRI mri)
        {
            int index = ItemTemp.MRIs.FindIndex(m => m.Equals(mri));
            if (index != -1)
            {
                ItemTemp.MRIs[index] = mri;
            }
        }

        protected void OnAddSite(Data.Site site)
        {
            if (!ItemTemp.Sites.Contains(site))
            {
                ItemTemp.Sites.Add(site);
            }
        }
        protected void OnRemoveSite(Data.Site site)
        {
            if (ItemTemp.Sites.Contains(site))
            {
                ItemTemp.Sites.Remove(site);
            }
        }
        protected void OnUpdateSite(Data.Site site)
        {
            int index = ItemTemp.Sites.FindIndex(s => s.Equals(site));
            if (index != -1)
            {
                ItemTemp.Sites[index] = site;
            }
        }

        protected void OnAddTag(Data.BaseTagValue tag)
        {
            if (!ItemTemp.Tags.Contains(tag))
            {
                ItemTemp.Tags.Add(tag);
            }
        }
        protected void OnRemoveTag(Data.BaseTagValue tag)
        {
            if (ItemTemp.Tags.Contains(tag))
            {
                ItemTemp.Tags.Remove(tag);
            }
        }
        protected void OnUpdateTag(Data.BaseTagValue tag)
        {
            int index = ItemTemp.Tags.FindIndex(t => t.Equals(tag));
            if (index != -1)
            {
                ItemTemp.Tags[index] = tag;
            }
        }
        #endregion
    }
}