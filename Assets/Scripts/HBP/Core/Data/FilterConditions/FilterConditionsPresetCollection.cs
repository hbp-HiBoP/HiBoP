using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FilterConditionsPresetCollection : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "FilterConditionsPresets.json");

        [JsonProperty] private List<FilterConditionsPreset> m_Presets = new List<FilterConditionsPreset>();
        public ReadOnlyCollection<FilterConditionsPreset> Presets => new ReadOnlyCollection<FilterConditionsPreset>(m_Presets);

        public FilterConditionsPreset CurrentPreset { get; set; } = new();
        #endregion

        #region Constructors
        public FilterConditionsPresetCollection(IEnumerable<FilterConditionsPreset> presets, string ID) : base(ID)
        {
            m_Presets = presets.ToList();
        }
        public FilterConditionsPresetCollection(IEnumerable<FilterConditionsPreset> presets) : base()
        {
            m_Presets = presets.ToList();
        }
        public FilterConditionsPresetCollection() : this(new List<FilterConditionsPreset>())
        {
        }
        #endregion

        #region Public Methods
        public static FilterConditionsPresetCollection Initialize()
        {
            FilterConditionsPresetCollection presetsCollection = new FilterConditionsPresetCollection();
            if (new FileInfo(PATH).Exists)
            {
                try
                {
                    presetsCollection = ClassLoaderSaver.LoadFromJson<FilterConditionsPresetCollection>(PATH);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    presetsCollection = new FilterConditionsPresetCollection();
                }
            }
            presetsCollection.Save();
            return presetsCollection;
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var alias in m_Presets) alias.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var alias in m_Presets) IDs.AddRange(alias.GetAllIdentifiable());
            return IDs;
        }
        public void Save()
        {
            ClassLoaderSaver.SaveToJSon(this, PATH, true);
        }
        public override object Clone()
        {
            return new FilterConditionsPresetCollection(m_Presets.DeepClone(), ID);
        }
        public override void Copy(object copy)
        {
            if (copy is FilterConditionsPresetCollection aliasCollection)
            {
                m_Presets = aliasCollection.m_Presets;
            }
        }
        public void AddPreset(FilterConditionsPreset presets, bool autoSave = true)
        {
            m_Presets.Add(presets);
            if (autoSave) Save();
        }
        public void RemovePreset(FilterConditionsPreset presets, bool autoSave = true)
        {
            m_Presets.Remove(presets);
            if (autoSave) Save();
        }
        public void SetPresets(IEnumerable<FilterConditionsPreset> presets, bool autoSave = true)
        {
            m_Presets = presets.ToList();
            if (autoSave) Save();
        }
        #endregion
    }
}