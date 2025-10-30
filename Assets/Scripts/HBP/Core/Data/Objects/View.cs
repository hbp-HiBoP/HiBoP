using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public struct View
    {
        #region Properties
        [JsonProperty] public SerializableVector3 Position { get; set; }
        [JsonProperty] public SerializableQuaternion Rotation { get; set; }
        [JsonProperty] public SerializableVector3 Target { get; set; }
        #endregion

        #region Constructors
        public View(Vector3 position, Quaternion rotation, Vector3 target)
        {
            Position = new SerializableVector3(position);
            Rotation = new SerializableQuaternion(rotation);
            Target = new SerializableVector3(target);
        }
        #endregion
    }
}
