using System;

namespace CRNL.HiBoP.RenderModel
{
    public readonly struct Float2 : IEquatable<Float2>
    {
        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }

        public bool Equals(Float2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Float2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public readonly struct Float3 : IEquatable<Float3>
    {
        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public bool Equals(Float3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is Float3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public readonly struct Rgba32 : IEquatable<Rgba32>
    {
        public Rgba32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public bool Equals(Rgba32 other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is Rgba32 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    }

    public readonly struct Matrix4x4F : IEquatable<Matrix4x4F>
    {
        public Matrix4x4F(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
        {
            M00 = m00;
            M01 = m01;
            M02 = m02;
            M03 = m03;
            M10 = m10;
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M20 = m20;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M30 = m30;
            M31 = m31;
            M32 = m32;
            M33 = m33;
        }

        public static Matrix4x4F Identity { get; } = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public float M00 { get; }
        public float M01 { get; }
        public float M02 { get; }
        public float M03 { get; }
        public float M10 { get; }
        public float M11 { get; }
        public float M12 { get; }
        public float M13 { get; }
        public float M20 { get; }
        public float M21 { get; }
        public float M22 { get; }
        public float M23 { get; }
        public float M30 { get; }
        public float M31 { get; }
        public float M32 { get; }
        public float M33 { get; }

        public bool IsFinite => Finite(M00) && Finite(M01) && Finite(M02) && Finite(M03) && Finite(M10) && Finite(M11) && Finite(M12) && Finite(M13) && Finite(M20) && Finite(M21) && Finite(M22) && Finite(M23) && Finite(M30) && Finite(M31) && Finite(M32) && Finite(M33);

        public bool Equals(Matrix4x4F other) => M00.Equals(other.M00) && M01.Equals(other.M01) && M02.Equals(other.M02) && M03.Equals(other.M03) && M10.Equals(other.M10) && M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) && M20.Equals(other.M20) && M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) && M30.Equals(other.M30) && M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33);

        public override bool Equals(object obj) => obj is Matrix4x4F other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(HashCode.Combine(M00, M01, M02, M03), HashCode.Combine(M10, M11, M12, M13), HashCode.Combine(M20, M21, M22, M23), HashCode.Combine(M30, M31, M32, M33));

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct Bounds3F
    {
        public Bounds3F(Float3 minimum, Float3 maximum)
        {
            if (!RenderMath.IsFinite(minimum) || !RenderMath.IsFinite(maximum))
                throw new ArgumentOutOfRangeException(nameof(minimum), "Bounds must be finite.");
            if (minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z)
                throw new ArgumentException("Bounds minimum must not exceed maximum.");

            Minimum = minimum;
            Maximum = maximum;
        }

        public Float3 Minimum { get; }
        public Float3 Maximum { get; }
    }

    public readonly struct Plane3F
    {
        public Plane3F(Float3 normal, float distance)
        {
            if (!RenderMath.IsFinite(normal) || !RenderMath.IsFinite(distance))
                throw new ArgumentOutOfRangeException(nameof(normal), "Plane values must be finite.");
            if (normal.X == 0f && normal.Y == 0f && normal.Z == 0f)
                throw new ArgumentException("Plane normal must be non-zero.", nameof(normal));

            Normal = normal;
            Distance = distance;
        }

        public Float3 Normal { get; }
        public float Distance { get; }
    }

    internal static class RenderMath
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static bool IsFinite(Float2 value) => IsFinite(value.X) && IsFinite(value.Y);
        public static bool IsFinite(Float3 value) => IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }
}
