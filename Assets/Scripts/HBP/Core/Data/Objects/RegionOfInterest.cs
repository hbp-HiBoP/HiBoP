using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct RegionOfInterest
    {
        #region Properties
        [JsonProperty] public string Name { get; set; }
        [JsonProperty] public List<Sphere> Spheres { get; set; }
        #endregion

        #region Constructors
        public RegionOfInterest(string name, List<Sphere> spheres)
        {
            Name = name;
            Spheres = spheres;
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct Sphere
    {
        #region Properties
        [JsonProperty] public SerializableVector3 Position { get; set; }
        [JsonProperty] public float Radius { get; set; }
        #endregion

        #region Constructors
        public Sphere(Vector3 position, float radius)
        {
            Position = new SerializableVector3(position);
            Radius = radius;
        }
        #endregion
    }
}