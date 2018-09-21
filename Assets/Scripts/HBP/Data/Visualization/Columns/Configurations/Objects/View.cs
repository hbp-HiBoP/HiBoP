using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class View
    {
        #region Properties
        [DataMember]
        public SerializableVector3 Position { get; set; }
        [DataMember]
        public SerializableQuaternion Rotation { get; set; }
        [DataMember]
        public SerializableVector3 Target { get; set; }
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
