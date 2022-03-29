using UnityEngine;
using System.Runtime.Serialization;

[DataContract]
public struct SerializableQuaternion
{
    [DataMember]
    float x;
    [DataMember]
    float y;
    [DataMember]
    float z;
    [DataMember]
    float w;

    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}