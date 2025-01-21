using HBP.Core.Data;
using HBP.Core.Tools;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Data.Database
{
    [DataContract]
    public class GlobalDatabaseSettings : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(ApplicationState.DatabasePath, "Settings.json");
        [DataMember] public bool IsFirstUse { get; set; }
        #endregion

        #region Constructors
        public GlobalDatabaseSettings(string ID) : base(ID)
        {
            IsFirstUse = false;
        }
        public GlobalDatabaseSettings() : base()
        {
            IsFirstUse = false;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new GlobalDatabaseSettings(ID)
            {
                IsFirstUse = IsFirstUse
            };
        }
        public void Copy(GlobalDatabaseSettings settings)
        {
            IsFirstUse = settings.IsFirstUse;
        }
        #endregion
    }
}