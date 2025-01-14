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
        [DataMember] public bool Initialized { get; set; }
        #endregion

        #region Constructors
        public GlobalDatabaseSettings(string ID) : base(ID)
        {
            Initialized = false;
        }
        public GlobalDatabaseSettings() : base()
        {
            Initialized = false;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new GlobalDatabaseSettings(ID)
            {
                Initialized = Initialized
            };
        }
        public void Copy(GlobalDatabaseSettings settings)
        {
            Initialized = settings.Initialized;
        }
        #endregion
    }
}