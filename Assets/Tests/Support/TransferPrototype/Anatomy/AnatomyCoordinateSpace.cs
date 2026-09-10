using System;

namespace HBP.Transfer.Anatomy
{
    public enum AnatomyHandedness : byte
    {
        Right = 1,
        Left = 2
    }

    public enum AnatomyLengthUnit : byte
    {
        Meter = 1,
        Millimeter = 2
    }

    public enum AnatomyWinding : byte
    {
        Clockwise = 1,
        CounterClockwise = 2
    }

    /// <summary>
    /// XYZ coordinates. AssetToBrain is row-major, acts on column vectors [x,y,z,1],
    /// and maps into the named anatomical frame in the same units. It never contains
    /// a camera or local presentation transform. Normals use its inverse transpose.
    /// Handedness describes the source asset axes; a negative mapping determinant
    /// reverses handedness in FrameId. Unit applies to source and destination axes.
    /// </summary>
    public sealed class AnatomyCoordinateSpace
    {
        public string FrameId { get; }
        public AnatomyHandedness Handedness { get; }
        public AnatomyLengthUnit Unit { get; }
        public float MetersPerUnit => Unit == AnatomyLengthUnit.Meter ? 1f : 0.001f;
        public uint MappingVersion { get; }
        public AnatomyBuffer<float> AssetToBrain { get; }

        public AnatomyCoordinateSpace(string frameId, AnatomyHandedness handedness, AnatomyLengthUnit unit, uint mappingVersion, float[] assetToBrain)
        {
            AnatomySnapshotCodec.ValidateText(frameId);
            if (handedness != AnatomyHandedness.Left && handedness != AnatomyHandedness.Right)
                throw new ArgumentOutOfRangeException(nameof(handedness));
            if (unit != AnatomyLengthUnit.Meter && unit != AnatomyLengthUnit.Millimeter)
                throw new ArgumentOutOfRangeException(nameof(unit));
            if (mappingVersion == 0) throw new ArgumentOutOfRangeException(nameof(mappingVersion));
            if (assetToBrain == null || assetToBrain.Length != 16)
                throw new ArgumentException("A row-major 4x4 matrix is required.", nameof(assetToBrain));
            var matrix = (float[])assetToBrain.Clone();
            AnatomySnapshot.ValidateFinite(matrix);
            double determinant = (double)matrix[0] * ((double)matrix[5] * matrix[10] - (double)matrix[6] * matrix[9]) - (double)matrix[1] * ((double)matrix[4] * matrix[10] - (double)matrix[6] * matrix[8]) + (double)matrix[2] * ((double)matrix[4] * matrix[9] - (double)matrix[5] * matrix[8]);
            if (matrix[12] != 0 || matrix[13] != 0 || matrix[14] != 0 || matrix[15] != 1 || determinant == 0)
                throw new ArgumentException("The anatomical mapping must be affine and invertible.", nameof(assetToBrain));
            FrameId = frameId;
            Handedness = handedness;
            Unit = unit;
            MappingVersion = mappingVersion;
            AssetToBrain = new AnatomyBuffer<float>(matrix);
        }
    }
}
