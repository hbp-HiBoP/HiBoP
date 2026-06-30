using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public struct RegionOfInterest : ICloneable
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

        #region Public Methods
        public object Clone()
        {
            return new RegionOfInterest(Name, Spheres?.DeepClone(true).ToList() ?? new List<Sphere>());
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public struct Sphere : ICloneable
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

        #region Public Methods
        public object Clone()
        {
            return new Sphere(Position.ToVector3(), Radius);
        }
        #endregion
    }
}
