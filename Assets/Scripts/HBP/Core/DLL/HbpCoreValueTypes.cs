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

    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec2
    {
        public float x;
        public float y;

        public static Vec2 FromVector2(Vector2 value)
        {
            return new Vec2 { x = value.x, y = value.y };
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Color4
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public static Color4 FromColor(Color value)
        {
            return new Color4 { r = value.r, g = value.g, b = value.b, a = value.a };
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TextureSize
    {
        public int width;
        public int height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VolumeDimensions
    {
        public int x;
        public int y;
        public int z;
        public int t;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VolumeExtrema
    {
        public float min;
        public float max;
        public float loadedCalMin;
        public float loadedCalMax;
        public float recomputedCalMin;
        public float recomputedCalMax;
    }
}
