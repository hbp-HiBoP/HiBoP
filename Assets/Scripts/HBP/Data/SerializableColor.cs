using UnityEngine;
using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract]
    public struct SerializableColor
    {
        [DataMember]
        float r;
        [DataMember]
        float g;
        [DataMember]
        float b;
        [DataMember]
        float a;

        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}