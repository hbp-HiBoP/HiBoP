using UnityEngine;
using System.Runtime.Serialization;

[DataContract]
public struct SerializableVector3
{
    [DataMember]
    float x;
    [DataMember]
    float y;
    [DataMember]
    float z;

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