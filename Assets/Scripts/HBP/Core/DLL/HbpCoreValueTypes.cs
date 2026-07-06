using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec3
    {
        public float x;
        public float y;
        public float z;

        public static Vec3 FromVector3(Vector3 value)
        {
            return new Vec3 { x = value.x, y = value.y, z = value.z };
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
