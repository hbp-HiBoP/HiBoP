using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public class Cut : Plane
    {
        #region Properties
        public const CutOrientation DEFAULT_ORIENTATION = CutOrientation.Axial;
        public const bool DEFAULT_FLIP = false;
        public const int DEFAULT_REMOVE_FRONT_PLANE = 0;
        public const int DEFAULT_NUMBER_OF_CUTS = 500;

        public int ID { get; set; }
        public CutOrientation Orientation { get; set; }
        public bool Flip { get; set; }
        public int RemoveFrontPlane { get; set; }
        public int NumberOfCuts { get; set; }
        #endregion

        #region Constructors
        public Cut() : base()
        {
            Orientation = DEFAULT_ORIENTATION;
            Flip = DEFAULT_FLIP;
            RemoveFrontPlane = DEFAULT_REMOVE_FRONT_PLANE;
            NumberOfCuts = DEFAULT_NUMBER_OF_CUTS;
        }

        public Cut(Vector3 point, Vector3 normal) : base(point, normal)
        {
            Orientation = DEFAULT_ORIENTATION;
            Flip = DEFAULT_FLIP;
            RemoveFrontPlane = DEFAULT_REMOVE_FRONT_PLANE;
            NumberOfCuts = DEFAULT_NUMBER_OF_CUTS;
        }
        #endregion
    }
}