using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TagCollection : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "Tags.json");
        public ReadOnlyCollection<BaseTag> AllTags
        {
            get
            {
                List<BaseTag> tags = new List<BaseTag>();
                tags.AddRange(GeneralTags);
                tags.AddRange(PatientsTags);
                tags.AddRange(SitesTags);
                return new ReadOnlyCollection<BaseTag>(tags);
            }
        }

        [JsonProperty] private List<BaseTag> m_GeneralTags;
        public ReadOnlyCollection<BaseTag> GeneralTags => new ReadOnlyCollection<BaseTag>(m_GeneralTags);

        [JsonProperty] private List<BaseTag> m_PatientsTags;
        public ReadOnlyCollection<BaseTag> PatientsTags => new ReadOnlyCollection<BaseTag>(m_PatientsTags);

        [JsonProperty] private List<BaseTag> m_SitesTags;
        public ReadOnlyCollection<BaseTag> SitesTags => new ReadOnlyCollection<BaseTag>(m_SitesTags);
        #endregion

        #region Constructors
        public TagCollection(IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags, string ID) : base(ID)
        {
            m_GeneralTags = generalTags.ToList();
            m_PatientsTags = patientsTags.ToList();
            m_SitesTags = sitesTags.ToList();
        }
        public TagCollection(IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags) : base()
        {
            m_GeneralTags = generalTags.ToList();
            m_PatientsTags = patientsTags.ToList();
            m_SitesTags = sitesTags.ToList();
        }
        public TagCollection() : this(new List<BaseTag>(), new List<BaseTag>(), new List<BaseTag>())
        {
        }
        #endregion

        #region Events
        public UnityEvent OnSaveTags = new UnityEvent();
        #endregion

        #region Public Methods
        public static TagCollection Initialize()
        {
            TagCollection tagsCollection = new TagCollection();
            if (new FileInfo(PATH).Exists)
            {
                try
                {
                    tagsCollection = ClassLoaderSaver.LoadFromJson<TagCollection>(PATH);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    tagsCollection = new TagCollection();
                }
            }
            tagsCollection.Save();
            return tagsCollection;
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var tag in m_GeneralTags) tag.GenerateID();
            foreach (var tag in m_PatientsTags) tag.GenerateID();
            foreach (var tag in m_SitesTags) tag.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var tag in m_GeneralTags) IDs.AddRange(tag.GetAllIdentifiable());
            foreach (var tag in m_PatientsTags) IDs.AddRange(tag.GetAllIdentifiable());
            foreach (var tag in m_SitesTags) IDs.AddRange(tag.GetAllIdentifiable());
            return IDs;
        }
        public void Save()
        {
            ClassLoaderSaver.SaveToJSon(this, PATH, true);
            OnSaveTags.Invoke();
        }
        public override object Clone()
        {
            return new TagCollection(m_GeneralTags.DeepClone(), m_PatientsTags.DeepClone(), m_SitesTags.DeepClone(), ID);
        }
        public override void Copy(object copy)
        {
            if (copy is TagCollection tagsCollection)
            {
                m_GeneralTags = tagsCollection.m_GeneralTags;
                m_PatientsTags = tagsCollection.m_PatientsTags;
                m_SitesTags = tagsCollection.m_SitesTags;
            }
        }
        public void AddGeneralTag(BaseTag tag, bool autoSave = true)
        {
            m_GeneralTags.Add(tag);
            if (autoSave) Save();
        }
        public void RemoveGeneralTag(BaseTag tag, bool autoSave = true)
        {
            m_GeneralTags.Remove(tag);
            if (autoSave) Save();
        }
        public void SetGeneralTags(IEnumerable<BaseTag> tags, bool autoSave = true)
        {
            m_GeneralTags = tags.ToList();
            if (autoSave) Save();
        }
        public void AddPatientTag(BaseTag tag, bool autoSave = true)
        {
            m_PatientsTags.Add(tag);
            if (autoSave) Save();
        }
        public void RemovePatientTag(BaseTag tag, bool autoSave = true)
        {
            m_PatientsTags.Remove(tag);
            if (autoSave) Save();
        }
        public void SetPatientTags(IEnumerable<BaseTag> tags, bool autoSave = true)
        {
            m_PatientsTags = tags.ToList();
            if (autoSave) Save();
        }
        public void AddSiteTag(BaseTag tag, bool autoSave = true)
        {
            m_SitesTags.Add(tag);
            if (autoSave) Save();
        }
        public void RemoveSiteTag(BaseTag tag, bool autoSave = true)
        {
            m_SitesTags.Remove(tag);
            if (autoSave) Save();
        }
        public void SetSiteTags(IEnumerable<BaseTag> tags, bool autoSave = true)
        {
            m_SitesTags = tags.ToList();
            if (autoSave) Save();
        }
        #endregion
    }
}