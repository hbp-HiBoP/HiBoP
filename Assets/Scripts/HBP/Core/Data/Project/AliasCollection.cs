using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class AliasCollection : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "Aliases.json");

        [JsonProperty] private List<Alias> m_Aliases = new();
        public ReadOnlyCollection<Alias> Aliases => new(m_Aliases);
        #endregion

        #region Constructors
        public AliasCollection(IEnumerable<Alias> aliases, string ID) : base(ID)
        {
            m_Aliases = aliases.ToList();
        }
        public AliasCollection(IEnumerable<Alias> aliases) : base()
        {
            m_Aliases = aliases.ToList();
        }
        public AliasCollection() : this(new List<Alias>())
        {
        }
        #endregion

        #region Events
        public UnityEvent OnSaveAliases = new();
        #endregion

        #region Public Methods
        public static AliasCollection Initialize()
        {
            AliasCollection aliasCollection = new();
            if (new FileInfo(PATH).Exists)
            {
                try
                {
                    var loadedAliasCollection = ClassLoaderSaver.LoadFromJson<AliasCollection>(PATH);
                    if (loadedAliasCollection != null)
                    {
                        aliasCollection = loadedAliasCollection;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    aliasCollection = new AliasCollection();
                }
            }
            else
            {
                aliasCollection.AddAlias(new Alias("[DATABASE_FOLDER]", Path.Combine(Application.persistentDataPath, "Database")));
            }
            aliasCollection.Save();
            return aliasCollection;
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var alias in m_Aliases) alias.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var alias in m_Aliases) IDs.AddRange(alias.GetAllIdentifiable());
            return IDs;
        }
        public void Save()
        {
            ClassLoaderSaver.SaveToJSon(this, PATH, true);
            OnSaveAliases.Invoke();
        }
        public override object Clone()
        {
            return new AliasCollection(m_Aliases.DeepClone(), ID);
        }
        public override void Copy(object copy)
        {
            if (copy is AliasCollection aliasCollection)
            {
                m_Aliases = aliasCollection.m_Aliases;
            }
        }
        public void AddAlias(Alias alias, bool autoSave = true)
        {
            m_Aliases.Add(alias);
            if (autoSave) Save();
        }
        public void RemoveAlias(Alias alias, bool autoSave = true)
        {
            m_Aliases.Remove(alias);
            if (autoSave) Save();
        }
        public void SetAliases(IEnumerable<Alias> aliases, bool autoSave = true)
        {
            m_Aliases = aliases.ToList();
            if (autoSave) Save();
        }
        #endregion
    }
}
