using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class CCEPConfiguration : DynamicConfiguration
    {
        #region Properties

        [JsonProperty] public bool UseMarsAtlas { get; set; }
        [JsonProperty] public string SiteID { get; set; }
        [JsonProperty] public int MarsAtlasLabel { get; set; }

        #endregion

        #region Constructors

        public CCEPConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax, float? spanMinRatio = null, float? middleRatio = null, float? spanMaxRatio = null, bool useMarsAtlas = false, string siteID = null, int marsAtlasLabel = -1) : this(maximumInfluence, spanMin, middle, spanMax, spanMinRatio, middleRatio, spanMaxRatio, useMarsAtlas, siteID, marsAtlasLabel, null)
        {
        }

        public CCEPConfiguration(float maximumInfluence, float spanMin, float middle, float spanMax, float? spanMinRatio, float? middleRatio, float? spanMaxRatio, bool useMarsAtlas, string siteID, int marsAtlasLabel, string ID) : base(maximumInfluence, spanMin, middle, spanMax, spanMinRatio, middleRatio, spanMaxRatio, ID)
        {
            UseMarsAtlas = useMarsAtlas;
            SiteID = siteID;
            MarsAtlasLabel = marsAtlasLabel;
        }

        public CCEPConfiguration() : this(15, 0, 0, 0)
        {
        }

        #endregion

        #region Public Methods

        public override object Clone()
        {
            return new CCEPConfiguration(MaximumInfluence, SpanMin, Middle, SpanMax, SpanMinRatio, MiddleRatio, SpanMaxRatio, UseMarsAtlas, SiteID, MarsAtlasLabel, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is CCEPConfiguration configuration)
            {
                UseMarsAtlas = configuration.UseMarsAtlas;
                SiteID = configuration.SiteID;
                MarsAtlasLabel = configuration.MarsAtlasLabel;
            }
        }

        #endregion
    }
}
