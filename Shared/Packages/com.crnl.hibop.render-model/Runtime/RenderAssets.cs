using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.RenderModel
{
    public enum SurfaceRepresentation : byte
    {
        Unknown = 0,
        Anatomical = 1,
        Inflated = 2,
        Other = 3,
    }

    public enum TextureColorSpace : byte
    {
        Unknown = 0,
        Linear = 1,
        Srgb = 2,
    }

    public sealed class SurfaceAsset
    {
        public SurfaceAsset(AssetHash hash, SurfaceRepresentation representation, CoordinateSpace coordinateSpace, Bounds3F bounds, RenderBuffer<Float3> positions, RenderBuffer<Float3> normals, RenderBuffer<uint> indices, RenderBuffer<Float2> staticUvs)
        {
            EnsureHash(hash, nameof(hash));
            if (representation == SurfaceRepresentation.Unknown)
                throw new ArgumentOutOfRangeException(nameof(representation));
            if (!coordinateSpace.IsValid)
                throw new ArgumentException("A valid coordinate space is required.", nameof(coordinateSpace));
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Indices = indices ?? throw new ArgumentNullException(nameof(indices));
            StaticUvs = staticUvs ?? throw new ArgumentNullException(nameof(staticUvs));
            if (Positions.Count == 0 || Normals.Count != Positions.Count)
                throw new ArgumentException("Surface positions and normals must have the same non-zero count.");
            if (StaticUvs.Count != 0 && StaticUvs.Count != Positions.Count)
                throw new ArgumentException("Static UVs must be empty or match the vertex count.", nameof(staticUvs));
            ValidateTriangleIndices(Indices, Positions.Count, nameof(indices));
            ValidateFinite(Positions, nameof(positions));
            ValidateFinite(Normals, nameof(normals));
            ValidateFinite(StaticUvs, nameof(staticUvs));

            Hash = hash;
            Representation = representation;
            CoordinateSpace = coordinateSpace;
            Bounds = bounds;
        }

        public AssetHash Hash { get; }
        public SurfaceRepresentation Representation { get; }
        public CoordinateSpace CoordinateSpace { get; }
        public Bounds3F Bounds { get; }
        public RenderBuffer<Float3> Positions { get; }
        public RenderBuffer<Float3> Normals { get; }
        public RenderBuffer<uint> Indices { get; }
        public RenderBuffer<Float2> StaticUvs { get; }

        internal static void EnsureHash(AssetHash hash, string parameterName)
        {
            if (!hash.IsValid)
                throw new ArgumentException("A valid asset hash is required.", parameterName);
        }

        internal static void ValidateTriangleIndices(RenderBuffer<uint> indices, int vertexCount, string parameterName)
        {
            if (indices.Count == 0 || indices.Count % 3 != 0)
                throw new ArgumentException("Triangle indices must contain complete triangles.", parameterName);
            for (int index = 0; index < indices.Count; index++)
            {
                if (indices[index] >= vertexCount)
                    throw new ArgumentOutOfRangeException(parameterName, "A triangle index is outside the vertex buffer.");
            }
        }

        internal static void ValidateFinite(RenderBuffer<Float3> values, string parameterName)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (!RenderMath.IsFinite(values[index]))
                    throw new ArgumentOutOfRangeException(parameterName, "Buffer values must be finite.");
            }
        }

        internal static void ValidateFinite(RenderBuffer<Float2> values, string parameterName)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (!RenderMath.IsFinite(values[index]))
                    throw new ArgumentOutOfRangeException(parameterName, "Buffer values must be finite.");
            }
        }
    }

    public sealed class SiteAsset
    {
        public SiteAsset(AssetHash hash, CoordinateSpace coordinateSpace, Bounds3F bounds, RenderBuffer<ContractId> siteIds, RenderBuffer<Float3> positions)
        {
            SurfaceAsset.EnsureHash(hash, nameof(hash));
            if (!coordinateSpace.IsValid)
                throw new ArgumentException("A valid coordinate space is required.", nameof(coordinateSpace));
            SiteIds = siteIds ?? throw new ArgumentNullException(nameof(siteIds));
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            if (SiteIds.Count == 0 || SiteIds.Count != Positions.Count)
                throw new ArgumentException("Site IDs and positions must have the same non-zero count.");

            HashSet<ContractId> uniqueIds = new();
            for (int index = 0; index < SiteIds.Count; index++)
            {
                if (!SiteIds[index].IsValid || !uniqueIds.Add(SiteIds[index]))
                    throw new ArgumentException("Site IDs must be valid and unique.", nameof(siteIds));
            }

            SurfaceAsset.ValidateFinite(Positions, nameof(positions));

            Hash = hash;
            CoordinateSpace = coordinateSpace;
            Bounds = bounds;
        }

        public AssetHash Hash { get; }
        public CoordinateSpace CoordinateSpace { get; }
        public Bounds3F Bounds { get; }
        public RenderBuffer<ContractId> SiteIds { get; }
        public RenderBuffer<Float3> Positions { get; }
    }

    public sealed class TextureAsset
    {
        public TextureAsset(AssetHash hash, int width, int height, TextureColorSpace colorSpace, RenderBuffer<Rgba32> pixels)
        {
            SurfaceAsset.EnsureHash(hash, nameof(hash));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (colorSpace == TextureColorSpace.Unknown)
                throw new ArgumentOutOfRangeException(nameof(colorSpace));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
            if (Pixels.Count != checked(width * height))
                throw new ArgumentException("Pixel count must match texture dimensions.", nameof(pixels));

            Hash = hash;
            Width = width;
            Height = height;
            ColorSpace = colorSpace;
        }

        public AssetHash Hash { get; }
        public int Width { get; }
        public int Height { get; }
        public TextureColorSpace ColorSpace { get; }
        public RenderBuffer<Rgba32> Pixels { get; }
    }

    public sealed class CutGeometryAsset
    {
        public CutGeometryAsset(AssetHash hash, CoordinateSpace coordinateSpace, Bounds3F bounds, RenderBuffer<Float3> positions, RenderBuffer<Float3> normals, RenderBuffer<Float2> uvs, RenderBuffer<uint> indices)
        {
            SurfaceAsset.EnsureHash(hash, nameof(hash));
            if (!coordinateSpace.IsValid)
                throw new ArgumentException("A valid coordinate space is required.", nameof(coordinateSpace));
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Uvs = uvs ?? throw new ArgumentNullException(nameof(uvs));
            Indices = indices ?? throw new ArgumentNullException(nameof(indices));
            if (Positions.Count == 0 || Normals.Count != Positions.Count || Uvs.Count != Positions.Count)
                throw new ArgumentException("Cut positions, normals and UVs must have the same non-zero count.");
            SurfaceAsset.ValidateTriangleIndices(Indices, Positions.Count, nameof(indices));
            SurfaceAsset.ValidateFinite(Positions, nameof(positions));
            SurfaceAsset.ValidateFinite(Normals, nameof(normals));
            SurfaceAsset.ValidateFinite(Uvs, nameof(uvs));

            Hash = hash;
            CoordinateSpace = coordinateSpace;
            Bounds = bounds;
        }

        public AssetHash Hash { get; }
        public CoordinateSpace CoordinateSpace { get; }
        public Bounds3F Bounds { get; }
        public RenderBuffer<Float3> Positions { get; }
        public RenderBuffer<Float3> Normals { get; }
        public RenderBuffer<Float2> Uvs { get; }
        public RenderBuffer<uint> Indices { get; }
    }
}
