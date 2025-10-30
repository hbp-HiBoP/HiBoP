using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public struct SerializableColor
    {
        [JsonProperty] float r;
        [JsonProperty] float g;
        [JsonProperty] float b;
        [JsonProperty] float a;

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