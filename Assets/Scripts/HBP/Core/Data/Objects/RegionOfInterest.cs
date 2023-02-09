using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Core.Data
{
    [DataContract]
    public struct RegionOfInterest
    {
        #region Properties
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<Sphere> Spheres { get; set; }
        #endregion

        #region Constructors
        public RegionOfInterest(string name, List<Sphere> spheres)
        {
            Name = name;
            Spheres = spheres;
        }
        #endregion
    }

    [DataContract]
    public struct Sphere
    {
        #region Properties
        [DataMember]
        public SerializableVector3 Position { get; set; }
        [DataMember]
        public float Radius { get; set; }
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