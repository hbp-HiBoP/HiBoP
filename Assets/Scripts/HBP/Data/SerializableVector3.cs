using UnityEngine;
using System.Runtime.Serialization;

[DataContract]
public struct SerializableVector3
{
    [DataMember] public float x { get; set; }
    [DataMember] public float y { get; set; }
    [DataMember] public float z { get; set; }

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