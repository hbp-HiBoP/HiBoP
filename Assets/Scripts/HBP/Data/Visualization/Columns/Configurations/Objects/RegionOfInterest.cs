 using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Data.Visualization
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
        public RegionOfInterest(Module3D.ROI roi)
        {
            Name = roi.Name;
            Spheres = new List<Sphere>();
            foreach (Module3D.Sphere sphere in roi.Spheres)
            {
                Spheres.Add(new Sphere(sphere.Position, sphere.Radius));
            }
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