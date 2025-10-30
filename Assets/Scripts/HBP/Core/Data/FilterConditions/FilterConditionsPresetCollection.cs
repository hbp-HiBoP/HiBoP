using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class FilterConditionsPresetCollection : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "FilterConditionsPresets.json");

        [JsonProperty] private Dictionary<Type, List<FilterConditionsPreset>> m_PresetsByType = new();
        [JsonProperty] private Dictionary<Type, FilterConditionsPreset> m_CurrentPresetByType = new();
        #endregion

        #region Public Methods
        public static FilterConditionsPresetCollection Initialize()
        {
            FilterConditionsPresetCollection presetsCollection = new FilterConditionsPresetCollection();
            if (new FileInfo(PATH).Exists)
            {
                try
                {
                    var loadedPresetCollection = ClassLoaderSaver.LoadFromJson<FilterConditionsPresetCollection>(PATH);
                    if (loadedPresetCollection != null)
                    {
                        presetsCollection = loadedPresetCollection;
                    }
                }
                catch (Exception e)
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
            foreach (var preset in m_PresetsByType.Values.SelectMany(v => v)) preset.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var preset in m_PresetsByType.Values.SelectMany(v => v)) IDs.AddRange(preset.GetAllIdentifiable());
            return IDs;
        }
        public void Save()
        {
            ClassLoaderSaver.SaveToJSon(this, PATH, true);
        }
        public override object Clone()
        {
            return new FilterConditionsPresetCollection()
            {
                m_PresetsByType = m_PresetsByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone().ToList())
            };
        }
        public override void Copy(object copy)
        {
            if (copy is FilterConditionsPresetCollection aliasCollection)
            {
                m_PresetsByType = aliasCollection.m_PresetsByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone().ToList());
            }
        }
        public void AddPreset(FilterConditionsPreset preset, Type type, bool autoSave = true)
        {
            if (!m_PresetsByType.ContainsKey(type)) m_PresetsByType[type] = new List<FilterConditionsPreset>();
            m_PresetsByType[type].Add(preset);
            if (autoSave) Save();
        }
        public void RemovePreset(FilterConditionsPreset presets, Type type, bool autoSave = true)
        {
            if (!m_PresetsByType.ContainsKey(type)) return;
            m_PresetsByType[type].Remove(presets);
            if (autoSave) Save();
        }
        public void SetPresets(IEnumerable<FilterConditionsPreset> presets, Type type, bool autoSave = true)
        {
            if (!m_PresetsByType.ContainsKey(type)) m_PresetsByType[type] = new List<FilterConditionsPreset>();
            m_PresetsByType[type] = presets.ToList();
            if (autoSave) Save();
        }
        public ReadOnlyCollection<FilterConditionsPreset> GetPresets(Type type)
        {
            return new ReadOnlyCollection<FilterConditionsPreset>(m_PresetsByType[type]);
        }
        public FilterConditionsPreset GetCurrentPreset(Type type)
        {
            if (!m_CurrentPresetByType.ContainsKey(type)) m_CurrentPresetByType[type] = new FilterConditionsPreset();
            return m_CurrentPresetByType[type];
        }
        public void SetCurrentPreset(FilterConditionsPreset preset, Type type, bool autoSave = true)
        {
            m_CurrentPresetByType[type] = preset;
            if (autoSave) Save();
        }
        #endregion
    }
}