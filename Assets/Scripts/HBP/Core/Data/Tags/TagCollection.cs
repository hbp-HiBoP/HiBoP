using HBP.Core.Tools;
using HBP.Core.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class TagCollection : BaseData
    {
        #region Properties

        public static string PATH = Path.Combine(Application.persistentDataPath, "Tags.json");
        private ReadOnlyCollection<BaseTag> m_AllTagsView;
        private Dictionary<string, BaseTag> m_TagById;
        public ReadOnlyCollection<BaseTag> AllTags => m_AllTagsView;

        [JsonProperty] private List<BaseTag> m_GeneralTags;
        private ReadOnlyCollection<BaseTag> m_GeneralTagsView;
        public ReadOnlyCollection<BaseTag> GeneralTags => m_GeneralTagsView;

        [JsonProperty] private List<BaseTag> m_PatientsTags;
        private ReadOnlyCollection<BaseTag> m_PatientsTagsView;
        public ReadOnlyCollection<BaseTag> PatientsTags => m_PatientsTagsView;

        [JsonProperty] private List<BaseTag> m_SitesTags;
        private ReadOnlyCollection<BaseTag> m_SitesTagsView;
        public ReadOnlyCollection<BaseTag> SitesTags => m_SitesTagsView;

        #endregion

        #region Constructors

        public TagCollection(IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags, string ID) : base(ID)
        {
            ApplyCollections(generalTags.ToList(), patientsTags.ToList(), sitesTags.ToList());
        }

        public TagCollection(IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags) : base()
        {
            ApplyCollections(generalTags.ToList(), patientsTags.ToList(), sitesTags.ToList());
        }

        public TagCollection() : this(new List<BaseTag>(), new List<BaseTag>(), new List<BaseTag>())
        {
        }

        #endregion

        #region Events

        public UnityEvent OnSaveTags = new();

        #endregion

        #region Private Methods

        private void ApplyCollections(List<BaseTag> generalTags, List<BaseTag> patientsTags, List<BaseTag> sitesTags)
        {
            generalTags ??= new();
            patientsTags ??= new();
            sitesTags ??= new();

            List<BaseTag> allTags = new(generalTags.Count + patientsTags.Count + sitesTags.Count);
            Dictionary<string, BaseTag> tagById = new(StringComparer.Ordinal);
            AddToIndex(patientsTags, allTags, tagById);
            AddToIndex(sitesTags, allTags, tagById);
            AddToIndex(generalTags, allTags, tagById);

            m_GeneralTags = generalTags;
            m_PatientsTags = patientsTags;
            m_SitesTags = sitesTags;
            m_GeneralTagsView = new ReadOnlyCollection<BaseTag>(m_GeneralTags);
            m_PatientsTagsView = new ReadOnlyCollection<BaseTag>(m_PatientsTags);
            m_SitesTagsView = new ReadOnlyCollection<BaseTag>(m_SitesTags);
            m_AllTagsView = new ReadOnlyCollection<BaseTag>(allTags);
            m_TagById = tagById;
        }

        private static void AddToIndex(IEnumerable<BaseTag> source, ICollection<BaseTag> allTags, IDictionary<string, BaseTag> tagById)
        {
            foreach (BaseTag tag in source)
            {
                allTags.Add(tag);
                if (tag == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(tag.ID))
                {
                    throw new InvalidOperationException("A tag cannot be indexed without an ID.");
                }

                if (tagById.TryGetValue(tag.ID, out BaseTag indexedTag))
                {
                    if (!ReferenceEquals(indexedTag, tag))
                    {
                        throw new InvalidOperationException($"Duplicate tag ID '{tag.ID}'.");
                    }
                }
                else
                {
                    tagById.Add(tag.ID, tag);
                }
            }
        }

        /// <summary>
        /// Analyzes a collection of values for a given tag name to determine the most appropriate tag type.
        /// Returns BoolTag if all values are booleans,
        /// IntTag if all values are integers (and at least one is not 0),
        /// FloatTag if all values are floats, otherwise StringTag.
        /// </summary>
        /// <param name="tagName">The name of the tag</param>
        /// <param name="values">Collection of string values to analyze</param>
        /// <returns>The most appropriate tag type</returns>
        private BaseTag CreateOptimalTag(string tagName, IEnumerable<string> values)
        {
            if (values == null || !values.Any())
                return new StringTag(tagName);

            var nonEmptyValues = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            if (!nonEmptyValues.Any())
                return new StringTag(tagName);

            bool allBooleans = true;
            bool allIntegers = true;
            bool hasNonZeroInteger = false;
            bool allFloats = true;

            foreach (var value in nonEmptyValues)
            {
                // Check for booleans first
                if (bool.TryParse(value, out bool boolValue))
                {
                    // Boolean values are also integers (in a sense), so don't break the chain yet
                    allIntegers = false; // But they're not really integers for tag purposes
                    allFloats = false; // And not floats either
                }
                else
                {
                    allBooleans = false;

                    // Check for integers
                    if (int.TryParse(value, out int intValue))
                    {
                        if (intValue != 0)
                            hasNonZeroInteger = true;
                        // Integer is also a valid float, so don't set allFloats to false yet
                    }
                    else
                    {
                        allIntegers = false;

                        // Check for floats
                        if (!NumberExtension.TryParseFloat(value, out float floatValue))
                        {
                            allFloats = false;
                            break; // If it's not a float, it must be a string
                        }
                    }
                }
            }

            // Decision logic:
            // 1. If all values are booleans, use BoolTag
            if (allBooleans)
                return new BoolTag(tagName);

            // 2. If all values are integers AND at least one is not zero, use IntTag
            if (allIntegers && hasNonZeroInteger)
                return new IntTag(tagName);

            // 3. If all values are valid floats (including integers), use FloatTag
            if (allFloats)
                return new FloatTag(tagName);

            // 4. Otherwise, use StringTag as fallback
            return new StringTag(tagName);
        }

        #endregion

        #region Public Methods

        public static TagCollection Initialize()
        {
            TagCollection tagsCollection = new();
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
            ApplyCollections(m_GeneralTags, m_PatientsTags, m_SitesTags);
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
                ApplyCollections(new List<BaseTag>(tagsCollection.m_GeneralTags), new List<BaseTag>(tagsCollection.m_PatientsTags), new List<BaseTag>(tagsCollection.m_SitesTags));
            }
        }

        public bool TryGetTag(string id, out BaseTag tag)
        {
            if (string.IsNullOrEmpty(id))
            {
                tag = null;
                return false;
            }

            return m_TagById.TryGetValue(id, out tag);
        }

        public bool ContainsTagId(string id)
        {
            return !string.IsNullOrEmpty(id) && m_TagById.ContainsKey(id);
        }

        public void AddGeneralTag(BaseTag tag, bool autoSave = true)
        {
            List<BaseTag> generalTags = new(m_GeneralTags) { tag };
            ApplyCollections(generalTags, m_PatientsTags, m_SitesTags);
            if (autoSave) Save();
        }

        public void RemoveGeneralTag(BaseTag tag, bool autoSave = true)
        {
            List<BaseTag> generalTags = new(m_GeneralTags);
            generalTags.Remove(tag);
            ApplyCollections(generalTags, m_PatientsTags, m_SitesTags);
            if (autoSave) Save();
        }

        public void SetGeneralTags(IEnumerable<BaseTag> tags, bool autoSave = true)
        {
            ApplyCollections(tags.ToList(), m_PatientsTags, m_SitesTags);
            if (autoSave) Save();
        }

        public void AddPatientTag(BaseTag tag, bool autoSave = true)
        {
            List<BaseTag> patientsTags = new(m_PatientsTags) { tag };
            ApplyCollections(m_GeneralTags, patientsTags, m_SitesTags);
            if (autoSave) Save();
        }

        public void RemovePatientTag(BaseTag tag, bool autoSave = true)
        {
            List<BaseTag> patientsTags = new(m_PatientsTags);
            patientsTags.Remove(tag);
            ApplyCollections(m_GeneralTags, patientsTags, m_SitesTags);
            if (autoSave) Save();
        }

        public void SetPatientTags(IEnumerable<BaseTag> tags, bool autoSave = true)
        {
            ApplyCollections(m_GeneralTags, tags.ToList(), m_SitesTags);
            if (autoSave) Save();
        }

        public void AddSiteTag(BaseTag tag, bool autoSave = true)
        {
            List<BaseTag> sitesTags = new(m_SitesTags) { tag };
            ApplyCollections(m_GeneralTags, m_PatientsTags, sitesTags);
            if (autoSave) Save();
        }

        public void RemoveSiteTag(BaseTag tag, bool autoSave = true)
        {
            List<BaseTag> sitesTags = new(m_SitesTags);
            sitesTags.Remove(tag);
            ApplyCollections(m_GeneralTags, m_PatientsTags, sitesTags);
            if (autoSave) Save();
        }

        public void SetSiteTags(IEnumerable<BaseTag> tags, bool autoSave = true)
        {
            ApplyCollections(m_GeneralTags, m_PatientsTags, tags.ToList());
            if (autoSave) Save();
        }

        public Dictionary<string, List<BaseTagValue>> GeneratePatientTagsFromCSV(string csvPath)
        {
            Regex csvParser = new(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            Dictionary<string, List<BaseTagValue>> resultTags = new();
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
                            // Collect all values for this column to determine optimal tag type
                            var columnValues = new List<string>();
                            for (int j = 1; j < lines.Length; j++)
                            {
                                string[] rowValues = csvParser.Split(lines[j]);
                                if (i < rowValues.Length)
                                {
                                    columnValues.Add(rowValues[i]);
                                }
                            }

                            tag = CreateOptimalTag(tagName, columnValues);
                            PersistentDataManager.Tags.AddPatientTag(tag);
                        }

                        tags[i - 1] = tag;
                    }

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] values = csvParser.Split(lines[i]);
                        string name = values.Length > 0 ? values[0] : "";
                        List<BaseTagValue> tagValues = new();
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
            Dictionary<string, List<BaseTagValue>> resultTags = new();
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
                            // Collect all values for this column to determine optimal tag type
                            var columnValues = new List<string>();
                            for (int j = 1; j < lines.Length; j++)
                            {
                                string[] rowValues = csvParser.Split(lines[j]);
                                if (i < rowValues.Length)
                                {
                                    columnValues.Add(rowValues[i]);
                                }
                            }

                            tag = CreateOptimalTag(tagName, columnValues);
                            PersistentDataManager.Tags.AddSiteTag(tag);
                        }

                        tags[i - 1] = tag;
                    }

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] values = csvParser.Split(lines[i]);
                        string name = values.Length > 0 ? values[0] : "";
                        List<BaseTagValue> tagValues = new();
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

        public Dictionary<string, List<BaseTagValue>> GeneratePatientTagsFromExcel(string excelPath)
        {
            Dictionary<string, List<BaseTagValue>> resultTags = new();
            if (!File.Exists(excelPath))
            {
                Debug.LogWarning($"Excel file not found: {excelPath}");
                return resultTags;
            }

            try
            {
                List<ExcelRowData> excelRows = ExcelReader.ReadExcelFileForPatientTags(excelPath);
                if (excelRows.Count == 0)
                {
                    Debug.LogWarning($"No data rows found in Excel file: {excelPath}");
                    return resultTags;
                }

                // Get all unique headers from all rows (since filtering may result in different headers per row)
                var allHeaders = new HashSet<string>();
                foreach (var row in excelRows)
                {
                    foreach (var header in row.GetHeaders())
                    {
                        allHeaders.Add(header);
                    }
                }

                string[] headers = allHeaders.ToArray();
                Dictionary<string, BaseTag> tagsByName = new();

                // Create or find tags for each header
                foreach (string tagName in headers)
                {
                    BaseTag tag = m_PatientsTags.Concat(m_GeneralTags).FirstOrDefault(t => t.Name == tagName);
                    if (tag == null)
                    {
                        // Collect all values for this tag to determine optimal tag type
                        var tagValues = new List<string>();
                        foreach (var row in excelRows)
                        {
                            if (row.TryGetValue(tagName, out string value))
                            {
                                tagValues.Add(value);
                            }
                        }

                        tag = CreateOptimalTag(tagName, tagValues);
                        PersistentDataManager.Tags.AddPatientTag(tag);
                    }

                    tagsByName[tagName] = tag;
                }

                // Process each data row
                foreach (var excelRow in excelRows)
                {
                    string name = excelRow.Name;
                    List<BaseTagValue> tagValues = new();

                    foreach (var headerName in excelRow.GetHeaders())
                    {
                        if (tagsByName.TryGetValue(headerName, out BaseTag tag))
                        {
                            if (excelRow.TryGetValue(headerName, out string value))
                            {
                                var tagValue = tag.CreateValue(value);
                                if (tagValue != null)
                                {
                                    tagValues.Add(tagValue);
                                }
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
            catch (Exception ex)
            {
                Debug.LogError($"Error processing Excel file {excelPath}: {ex.Message}");
            }

            return resultTags;
        }

        public Dictionary<string, List<BaseTagValue>> GenerateSiteTagsFromExcel(string excelPath)
        {
            Dictionary<string, List<BaseTagValue>> resultTags = new();
            if (!File.Exists(excelPath))
            {
                Debug.LogWarning($"Excel file not found: {excelPath}");
                return resultTags;
            }

            try
            {
                List<ExcelRowData> excelRows = ExcelReader.ReadExcelFileForSiteTags(excelPath);
                if (excelRows.Count == 0)
                {
                    Debug.LogWarning($"No data rows found in Excel file: {excelPath}");
                    return resultTags;
                }

                // Get all unique headers from all rows (since filtering may result in different headers per row)
                var allHeaders = new HashSet<string>();
                foreach (var row in excelRows)
                {
                    foreach (var header in row.GetHeaders())
                    {
                        allHeaders.Add(header);
                    }
                }

                string[] headers = allHeaders.ToArray();
                Dictionary<string, BaseTag> tagsByName = new();

                // Create or find tags for each header
                foreach (string tagName in headers)
                {
                    BaseTag tag = m_SitesTags.Concat(m_GeneralTags).FirstOrDefault(t => t.Name == tagName);
                    if (tag == null)
                    {
                        // Collect all values for this tag to determine optimal tag type
                        var tagValues = new List<string>();
                        foreach (var row in excelRows)
                        {
                            if (row.TryGetValue(tagName, out string value))
                            {
                                tagValues.Add(value);
                            }
                        }

                        tag = CreateOptimalTag(tagName, tagValues);
                        PersistentDataManager.Tags.AddSiteTag(tag);
                    }

                    tagsByName[tagName] = tag;
                }

                // Process each data row
                foreach (var excelRow in excelRows)
                {
                    string name = excelRow.Name;
                    List<BaseTagValue> tagValues = new();

                    foreach (var headerName in excelRow.GetHeaders())
                    {
                        if (tagsByName.TryGetValue(headerName, out BaseTag tag))
                        {
                            if (excelRow.TryGetValue(headerName, out string value))
                            {
                                var tagValue = tag.CreateValue(value);
                                if (tagValue != null)
                                {
                                    tagValues.Add(tagValue);
                                }
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
            catch (Exception ex)
            {
                Debug.LogError($"Error processing Excel file {excelPath}: {ex.Message}");
            }

            return resultTags;
        }

        #endregion

        #region Serialization

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            ApplyCollections(m_GeneralTags, m_PatientsTags, m_SitesTags);
        }

        #endregion
    }
}
