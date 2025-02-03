using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.IO;

namespace HBP.Data.Database
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GlobalDatabaseSettings : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(ApplicationState.DatabasePath, "Settings.json");
        [JsonProperty] public bool IsFirstUse { get; set; }
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