using UnityEngine;
using System;

namespace HBP.Data.Visualisation
{
    public abstract class Volume : ICloneable
    {
        public abstract object Clone();
        public abstract bool IsInVolume(Vector3 position);
    }
}