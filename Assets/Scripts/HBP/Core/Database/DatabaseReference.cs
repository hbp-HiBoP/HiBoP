using HBP.Core.Data;
using Newtonsoft.Json;
using System;
using UnityEngine.Scripting;

namespace HBP.Core.Database
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class DatabaseReference : BaseData
    {
        #region Properties
        public const string EXTENSION = ".hibopdb";
        [JsonProperty("Name")] public string Name { get; set; }
        [JsonProperty("Type")] public DatabaseType Type { get; set; }
        [JsonProperty("Path")] public string Path { get; set; }
        [JsonProperty("Parameters")] public DatabaseReferenceParameters Parameters { get; set; }
        [JsonProperty("LastUpdated")] public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        #endregion

        #region Constructors
        public DatabaseReference(string name, DatabaseType type, string path, DatabaseReferenceParameters parameters, DateTime lastUpdated, string ID) : base(ID)
        {
            Name = name;
            Type = type;
            Path = path;
            Parameters = parameters;
            LastUpdated = lastUpdated;
        }
        public DatabaseReference(string name, DatabaseType type, string path, DatabaseReferenceParameters parameters, DateTime lastUpdated) : base()
        {
            Name = name;
            Type = type;
            Path = path;
            Parameters = parameters;
            LastUpdated = lastUpdated;
        }
        public DatabaseReference() : this("New Database", DatabaseType.Brainvisa, "", null, DateTime.MinValue)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new DatabaseReference(Name, Type, Path, Parameters?.Clone() as DatabaseReferenceParameters, LastUpdated, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DatabaseReference databaseReference)
            {
                Name = databaseReference.Name;
                Type = databaseReference.Type;
                Path = databaseReference.Path;
                Parameters = databaseReference.Parameters;
                LastUpdated = databaseReference.LastUpdated;
            }
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public abstract class DatabaseReferenceParameters : BaseData
    {

    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class BrainvisaDatabaseParameters : DatabaseReferenceParameters
    {
        public override object Clone()
        {
            return new BrainvisaDatabaseParameters();
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class LocalizerDatabaseParameters : DatabaseReferenceParameters
    {
        [JsonProperty] public bool IncludeRaw { get; set; } = false;
        [JsonProperty] public string[] Frequencies { get; set; } = new string[] { "f8f24", "f50f150" };
        [JsonProperty] public string[] TemporalSmoothings { get; set; } = new string[] { "sm0", "sm250", "sm500", "sm1000", "sm2500", "sm5000" };

        public override object Clone()
        {
            return new LocalizerDatabaseParameters() { IncludeRaw = IncludeRaw, Frequencies = Frequencies, TemporalSmoothings = TemporalSmoothings };
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is LocalizerDatabaseParameters localizerDatabaseParameters)
            {
                IncludeRaw = localizerDatabaseParameters.IncludeRaw;
                Frequencies = localizerDatabaseParameters.Frequencies;
                TemporalSmoothings = localizerDatabaseParameters.TemporalSmoothings;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class BIDSDatabaseParameters : DatabaseReferenceParameters
    {
        public override object Clone()
        {
            return new BIDSDatabaseParameters();
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class TagsDatabaseParameters : DatabaseReferenceParameters
    {
        public override object Clone()
        {
            return new TagsDatabaseParameters();
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
    }
}