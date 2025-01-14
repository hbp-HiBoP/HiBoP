using HBP.Core.Data;
using System;
using System.Runtime.Serialization;

namespace HBP.Data.Database
{
    [DataContract]
    public class DatabaseReference : BaseData
    {
        #region Properties
        public const string EXTENSION = ".hibopdb";
        [DataMember(Name = "Name")] public string Name { get; set; }
        [DataMember(Name = "Type")] public DatabaseType Type { get; set; }
        [DataMember(Name = "Path")] public string Path { get; set; }
        [DataMember(Name = "LastUpdated")] public DateTime LastUpdated { get; set; }
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
        public DatabaseReference() : this("New Database", DatabaseType.Brainvisa, "", DateTime.Now)
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