using HBP.Core.Data;
using Newtonsoft.Json;
using System;

namespace HBP.Data.Database
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DatabaseReference : BaseData
    {
        #region Properties
        public const string EXTENSION = ".hibopdb";
        [JsonProperty("Name")] public string Name { get; set; }
        [JsonProperty("Type")] public DatabaseType Type { get; set; }
        [JsonProperty("Path")] public string Path { get; set; }
        [JsonProperty("LastUpdated")] public DateTime LastUpdated { get; set; }
        #endregion

        #region Constructors
        public DatabaseReference(string name, DatabaseType type, string path, DateTime lastUpdated, string ID) : base(ID)
        {
            Name = name;
            Type = type;
            Path = path;
            LastUpdated = lastUpdated;
        }
        public DatabaseReference(string name, DatabaseType type, string path, DateTime lastUpdated) : base()
        {
            Name = name;
            Type = type;
            Path = path;
            LastUpdated = lastUpdated;
        }
        public DatabaseReference() : this("New Database", DatabaseType.Brainvisa, "", DateTime.MinValue)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new DatabaseReference(Name, Type, Path, LastUpdated, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DatabaseReference databaseReference)
            {
                Name = databaseReference.Name;
                Type = databaseReference.Type;
                Path = databaseReference.Path;
                LastUpdated = databaseReference.LastUpdated;
            }
        }
        #endregion
    }
}