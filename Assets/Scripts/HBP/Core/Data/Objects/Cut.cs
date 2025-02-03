using Newtonsoft.Json;
using UnityEngine;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct Cut
    {
        #region Properties
        [JsonProperty] public SerializableVector3 Normal { get; set; }
        [JsonProperty] public Enums.CutOrientation Orientation { get; set; }
        [JsonProperty] public bool Flip { get; set; }
        [JsonProperty] public float Position { get; set; }
        #endregion

        #region Constructors
        public Cut(Vector3 normal, Enums.CutOrientation orientation, bool flip, float position)
        {
            Normal = new SerializableVector3(normal);
            Orientation = orientation;
            Flip = flip;
            Position = position;
        }
        #endregion
    }
}