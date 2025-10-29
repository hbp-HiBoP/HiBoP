using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
                tags.AddRange(PatientsTags);
                tags.AddRange(SitesTags);
                tags.AddRange(GeneralTags);
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

        #region Private Methods
        
        #endregion

        #region Public Methods
        public static TagCollection Initialize()
        {
            TagCollection tagsCollection = new TagCollection();
            if (new FileInfo(PATH).Exists)
            {
                try
                {
                    var loadedTagsCollection = ClassLoaderSaver.LoadFromJson<TagCollection>(PATH);
                    if (loadedTagsCollection != null)
                    {
                        tagsCollection = loadedTagsCollection;
                    }
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
        public Dictionary<string, List<BaseTagValue>> GeneratePatientTagsFromCSV(string csvPath)
        {
            Regex csvParser = new(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            Dictionary<string, List<BaseTagValue>> resultTags = new Dictionary<string, List<BaseTagValue>>();
            if (File.Exists(csvPath))
            {
                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length > 0)
                {
                    string[] headers = csvParser.Split(lines[0]);
                    BaseTag[] tags = new BaseTag[headers.Length - 1];
                    for (int i = 1; i < headers.Length; i++)
                    {
                        string tagName = headers[i];
                        BaseTag tag = m_PatientsTags.Concat(m_GeneralTags).FirstOrDefault(t => t.Name == tagName);
                        if (tag == null)
                        {
                            tag = new StringTag(tagName);
                            PersistentDataManager.Tags.AddPatientTag(tag);
                        }
                        tags[i - 1] = tag;
                    }
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] values = csvParser.Split(lines[i]);
                        string name = values.Length > 0 ? values[0] : "";
                        List<BaseTagValue> tagValues = new List<BaseTagValue>();
                        for (int j = 1; j < values.Length; j++)
                        {
                            BaseTag tag = tags[j - 1];
                            if (tag != null)
                            {
                                var tagValue = tag.CreateValue(values[j]);
                                if (tagValue != null)
                                {
                                    tagValues.Add(tagValue);
                                }
                            }
                        }
                        if (!resultTags.ContainsKey(name))
                        {
                            resultTags.Add(name, tagValues);
                        }
                        else
                        {
                            resultTags[name] = tagValues;
                        }
                    }
                }
            }
            return resultTags;
        }
        public Dictionary<string, List<BaseTagValue>> GenerateSiteTagsFromCSV(string csvPath)
        {
            Regex csvParser = new(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            Dictionary<string, List<BaseTagValue>> resultTags = new Dictionary<string, List<BaseTagValue>>();
            if (File.Exists(csvPath))
            {
                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length > 0)
                {
                    string[] headers = csvParser.Split(lines[0]);
                    BaseTag[] tags = new BaseTag[headers.Length - 1];
                    for (int i = 1; i < headers.Length; i++)
                    {
                        string tagName = headers[i];
                        BaseTag tag = m_SitesTags.Concat(m_GeneralTags).FirstOrDefault(t => t.Name == tagName);
                        if (tag == null)
                        {
                            tag = new StringTag(tagName);
                            PersistentDataManager.Tags.AddSiteTag(tag);
                        }
                        tags[i - 1] = tag;
                    }
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] values = csvParser.Split(lines[i]);
                        string name = values.Length > 0 ? values[0] : "";
                        List<BaseTagValue> tagValues = new List<BaseTagValue>();
                        for (int j = 1; j < values.Length; j++)
                        {
                            BaseTag tag = tags[j - 1];
                            if (tag != null)
                            {
                                var tagValue = tag.CreateValue(values[j]);
                                if (tagValue != null)
                                {
                                    tagValues.Add(tagValue);
                                }
                            }
                        }
                        if (!resultTags.ContainsKey(name))
                        {
                            resultTags.Add(name, tagValues);
                        }
                        else
                        {
                            resultTags[name] = tagValues;
                        }
                    }
                }
            }
            return resultTags;
        }
        public async UniTask CheckTagsAsync(IEnumerable<BaseTag> tags)
        {
            await UniTask.SwitchToThreadPool();
            List<Patient> patients = new List<Patient>();
            if (ApplicationState.LoadedProject != null) patients.AddRange(ApplicationState.LoadedProject.Patients);
            if (DatabaseManager.Database.IsLoaded) patients.AddRange(DatabaseManager.Database.Patients);

            var tasks = patients.Select(patient => (Func<UniTask>)(async () =>
            {
                await patient.CheckTagsAsync(tags);
            }));
            await LoadingManager.LoadAsync(async update => await Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Checking patients", update, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading));
        }
        #endregion
    }
}