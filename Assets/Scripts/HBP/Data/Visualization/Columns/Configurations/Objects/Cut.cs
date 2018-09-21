using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Data.Visualization
{
    [DataContract]
    public struct Cut
    {
        #region Properties
        [DataMember]
        public SerializableVector3 Normal { get; set; }
        [DataMember]
        public Data.Enums.CutOrientation Orientation { get; set; }
        [DataMember]
        public bool Flip { get; set; }
        [DataMember]
        public float Position { get; set; }
        #endregion

        #region Constructors
        public Cut(Vector3 normal, Data.Enums.CutOrientation orientation, bool flip, float position)
        {
            Normal = new SerializableVector3(normal);
            Orientation = orientation;
            Flip = flip;
            Position = position;
        }
        #endregion
    }
}