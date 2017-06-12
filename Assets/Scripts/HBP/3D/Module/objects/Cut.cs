using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public class Cut : Plane
    {
        #region Properties
        public const CutOrientation DEFAULT_ORIENTATION = CutOrientation.Axial;
        public const bool DEFAULT_FLIP = false;
        public const int DEFAULT_REMOVE_FRONT_PLANE = 0;
        public const int DEFAULT_NUMBER_OF_CUTS = 500;
        public const float DEFAULT_POSITION = 0.5f;

        public int ID { get; set; }
        public CutOrientation Orientation { get; set; }
        public bool Flip { get; set; }
        public int RemoveFrontPlane { get; set; }
        public int NumberOfCuts { get; set; }
        public float Position { get; set; }

        public GenericEvent<Texture2D> OnUpdateGUITextures = new GenericEvent<Texture2D>();
        #endregion

        #region Constructors
        public Cut() : base()
        {
            Orientation = DEFAULT_ORIENTATION;
            Flip = DEFAULT_FLIP;
            RemoveFrontPlane = DEFAULT_REMOVE_FRONT_PLANE;
            NumberOfCuts = DEFAULT_NUMBER_OF_CUTS;
            Position = DEFAULT_POSITION;
        }

        public Cut(Vector3 point, Vector3 normal) : base(point, normal)
        {
            Orientation = DEFAULT_ORIENTATION;
            Flip = DEFAULT_FLIP;
            RemoveFrontPlane = DEFAULT_REMOVE_FRONT_PLANE;
            NumberOfCuts = DEFAULT_NUMBER_OF_CUTS;
            Position = DEFAULT_POSITION;
        }
        #endregion
    }
}