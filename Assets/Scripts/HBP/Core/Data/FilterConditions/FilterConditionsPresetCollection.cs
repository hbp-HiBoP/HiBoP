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
        [JsonProperty] private Dictionary<Type, List<FilterConditionsPreset>> m_DisabledPresetsByType = new();
        [JsonIgnore] public bool HasUnsavedTagMigration { get; private set; }
        [JsonIgnore] public Exception InitializationException { get; private set; }
        [JsonIgnore] public int DisabledPresetCount => (m_DisabledPresetsByType ?? new()).Values.Sum(presets => presets?.Count ?? 0);

        #endregion

        #region Public Methods

        public static FilterConditionsPresetCollection Initialize()
        {
            return Initialize(out _);
        }

        internal static FilterConditionsPresetCollection Initialize(out Exception initializationException)
        {
            initializationException = null;
            FilterConditionsPresetCollection presetsCollection = new();
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
                    initializationException = e;
                    presetsCollection = new FilterConditionsPresetCollection { InitializationException = e };
                }
            }

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
            if (InitializationException != null) throw new InvalidOperationException("The original filter preset file is invalid and was preserved. Restore it and restart HiBoP before saving filters.", InitializationException);
            ClassLoaderSaver.SaveToJsonAtomicOrThrow(this, PATH);
            HasUnsavedTagMigration = false;
        }

        public void SaveRecovered()
        {
            string backupPath = PATH + ".pre-recovery.bak";
            if (File.Exists(PATH)) File.Copy(PATH, backupPath, true);
            Save();
        }

        internal void MarkTagMigrationUnsaved()
        {
            HasUnsavedTagMigration = true;
        }

        internal void ResolveReferences(LoadingContext context)
        {
            IEnumerable<FilterConditionsPreset> presets = m_PresetsByType.Values.SelectMany(value => value).Concat(m_CurrentPresetByType.Values).Where(preset => preset != null).Distinct();

            foreach (FilterConditionsPreset preset in presets)
            {
                foreach (BaseFilterCondition condition in preset.Conditions ?? Enumerable.Empty<BaseFilterCondition>())
                {
                    context.ResolveFilterCondition(condition);
                }
            }
        }

        internal IEnumerable<BaseFilterCondition> EnumerateConditions()
        {
            return m_PresetsByType.Values.SelectMany(value => value).Concat(m_CurrentPresetByType.Values).Where(preset => preset != null).Distinct().SelectMany(preset => preset.Conditions ?? Enumerable.Empty<BaseFilterCondition>());
        }

        internal IEnumerable<(Type Type, FilterConditionsPreset Preset)> GetNamedPresetEntries()
        {
            return (m_PresetsByType ?? new()).SelectMany(pair => (pair.Value ?? new()).Where(preset => preset != null).Select(preset => (pair.Key, preset)));
        }

        internal IEnumerable<(Type Type, FilterConditionsPreset Preset)> GetCurrentPresetEntries()
        {
            return (m_CurrentPresetByType ?? new()).Where(pair => pair.Value != null).Select(pair => (pair.Key, pair.Value));
        }

        internal IEnumerable<(Type Type, FilterConditionsPreset Preset)> GetDisabledPresetEntries()
        {
            return (m_DisabledPresetsByType ?? new()).SelectMany(pair => (pair.Value ?? new()).Where(preset => preset != null).Select(preset => (pair.Key, preset)));
        }

        internal void ReplaceNamedPreset(Type type, FilterConditionsPreset source, FilterConditionsPreset replacement)
        {
            if (!m_PresetsByType.TryGetValue(type, out List<FilterConditionsPreset> presets)) return;
            int index = presets.IndexOf(source);
            if (index >= 0) presets[index] = replacement;
        }

        internal void ReplaceCurrentPreset(Type type, FilterConditionsPreset replacement)
        {
            m_CurrentPresetByType[type] = replacement;
        }

        internal void QuarantineNamedPreset(Type type, FilterConditionsPreset preset)
        {
            if (m_PresetsByType.TryGetValue(type, out List<FilterConditionsPreset> presets)) presets.Remove(preset);
            AddDisabledPreset(type, preset);
        }

        internal void QuarantineCurrentPreset(Type type, FilterConditionsPreset preset)
        {
            AddDisabledPreset(type, preset);
            m_CurrentPresetByType[type] = new FilterConditionsPreset();
        }

        private void AddDisabledPreset(Type type, FilterConditionsPreset preset)
        {
            m_DisabledPresetsByType ??= new();
            if (!m_DisabledPresetsByType.TryGetValue(type, out List<FilterConditionsPreset> presets))
            {
                presets = new();
                m_DisabledPresetsByType[type] = presets;
            }

            if (preset != null) presets.Add((FilterConditionsPreset)preset.Clone());
        }

        internal string GetMigrationSignature()
        {
            JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.Auto };
            return JsonConvert.SerializeObject(this, Formatting.None, settings);
        }

        internal object CaptureMigrationState()
        {
            return new MigrationState(m_PresetsByType, m_CurrentPresetByType, m_DisabledPresetsByType);
        }

        internal void RestoreMigrationState(object state)
        {
            if (state is not MigrationState migrationState) throw new ArgumentException("Invalid filter preset migration state.", nameof(state));
            m_PresetsByType = migrationState.PresetsByType;
            m_CurrentPresetByType = migrationState.CurrentPresetByType;
            m_DisabledPresetsByType = migrationState.DisabledPresetsByType;
        }

        public override object Clone()
        {
            return new FilterConditionsPresetCollection()
            {
                m_PresetsByType = m_PresetsByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone().ToList()),
                m_CurrentPresetByType = m_CurrentPresetByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.Clone() as FilterConditionsPreset),
                m_DisabledPresetsByType = (m_DisabledPresetsByType ?? new()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone().ToList())
            };
        }

        public override void Copy(object copy)
        {
            if (copy is FilterConditionsPresetCollection aliasCollection)
            {
                m_PresetsByType = aliasCollection.m_PresetsByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone().ToList());
                m_CurrentPresetByType = aliasCollection.m_CurrentPresetByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.Clone() as FilterConditionsPreset);
                m_DisabledPresetsByType = (aliasCollection.m_DisabledPresetsByType ?? new()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone().ToList());
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

        private sealed class MigrationState
        {
            public Dictionary<Type, List<FilterConditionsPreset>> PresetsByType { get; }
            public Dictionary<Type, FilterConditionsPreset> CurrentPresetByType { get; }
            public Dictionary<Type, List<FilterConditionsPreset>> DisabledPresetsByType { get; }

            public MigrationState(Dictionary<Type, List<FilterConditionsPreset>> presetsByType, Dictionary<Type, FilterConditionsPreset> currentPresetByType, Dictionary<Type, List<FilterConditionsPreset>> disabledPresetsByType)
            {
                PresetsByType = presetsByType;
                CurrentPresetByType = currentPresetByType;
                DisabledPresetsByType = disabledPresetsByType;
            }
        }

        #endregion
    }
}
