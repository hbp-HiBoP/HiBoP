using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.DLL;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Specific location"), SortingOrder(6), FilterCondition(typeof(Object3D.Site))]
    public class SpecificSiteLocationFilterCondition : BaseFilterCondition
    {
        public static Func<SpecificSiteLocationFilterCondition, Object3D.Site, bool?> SceneLocationEvaluator { get; set; }

        #region Enums
        public enum SpecificLocationType { BrainMesh, Atlas, RegionOfInterest, CutPlane  }
        public enum Atlas { MarsAtlas, Jubrain }
        #endregion

        #region Properties
        [JsonProperty("LocationType")] public SpecificLocationType LocationType { get; set; }
        [JsonProperty("MeshPart")] public MeshPart MeshPart { get; set; } = MeshPart.Both;
        [JsonProperty("AtlasType")] public Atlas AtlasType { get; set; }
        [JsonProperty("AtlasArea")] public string AtlasArea { get; set; }

        public override string Description
        {
            get
            {
                switch (LocationType)
                {
                    case SpecificLocationType.BrainMesh:
                        string meshPart = MeshPart switch
                        {
                            MeshPart.Both => "the brain mesh",
                            MeshPart.Left => "the left hemisphere",
                            MeshPart.Right => "the right hemisphere",
                            _ => "the brain mesh"
                        };
                        return $"The site is {(IsNot ? "outside of" : "within")} {meshPart}";
                    case SpecificLocationType.Atlas:
                        return $"The site is {(IsNot ? "outside of the" : "inside the")} \"{AtlasArea}\" area of the \"{AtlasType}\" atlas";
                    case SpecificLocationType.RegionOfInterest:
                        return $"The site is {(IsNot ? "outside of the" : "inside the")} selected region of interest";
                    case SpecificLocationType.CutPlane:
                        return $"The site is{(IsNot ? " not" : "")} located on a cut plane";
                    default:
                        return "Invalid condition";
                }
            }
        }
        #endregion

        #region Constructors
        public SpecificSiteLocationFilterCondition() : this(SpecificLocationType.BrainMesh, MeshPart.Both, Atlas.MarsAtlas, "", false) { }
        public SpecificSiteLocationFilterCondition(SpecificLocationType locationType, MeshPart meshPart, Atlas atlasType, string atlasArea, bool isNot) : base(isNot)
        {
            LocationType = locationType;
            MeshPart = meshPart;
            AtlasType = atlasType;
            AtlasArea = atlasArea;
        }
        public SpecificSiteLocationFilterCondition(SpecificLocationType locationType, MeshPart meshPart, Atlas atlasType, string atlasArea, bool isNot, string ID) : base(isNot, ID)
        {
            LocationType = locationType;
            MeshPart = meshPart;
            AtlasType = atlasType;
            AtlasArea = atlasArea;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SpecificSiteLocationFilterCondition(LocationType, MeshPart, AtlasType, AtlasArea, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is SpecificSiteLocationFilterCondition other)
            {
                LocationType = other.LocationType;
                MeshPart = other.MeshPart;
                AtlasType = other.AtlasType;
                AtlasArea = other.AtlasArea;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is Object3D.Site site)
            {
                bool result = false;
                switch (LocationType)
                {
                    case SpecificLocationType.BrainMesh:
                        result = SceneLocationEvaluator?.Invoke(this, site) ?? false;
                        break;
                    case SpecificLocationType.Atlas:
                        BrainAtlas atlas = AtlasType switch
                        {
                            Atlas.MarsAtlas => Object3DManager.MarsAtlas,
                            Atlas.Jubrain => Object3DManager.JuBrain,
                            _ => null
                        };

                        if (atlas == null)
                            return false;

                        if (!atlas.Loaded)
                            atlas.Load();

                        int areaID = atlas.GetClosestAreaIndex(site.Information.DefaultPosition, 2);

                        if (areaID == -1)
                            return false;

                        result = atlas.GetAreaName(areaID) == AtlasArea;
                        break;
                    case SpecificLocationType.RegionOfInterest:
                        result = !site.State.IsOutOfROI;
                        break;
                    case SpecificLocationType.CutPlane:
                        result = SceneLocationEvaluator?.Invoke(this, site) ?? false;
                        break;
                }
                return result != IsNot;
            }
            return false;
        }
        #endregion
    }
}
