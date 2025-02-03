using UnityEngine;
using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct SerializableVector3
    {
        [JsonProperty] public float x { get; set; }
        [JsonProperty] public float y { get; set; }
        [JsonProperty] public float z { get; set; }

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public SerializableVector3(Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}