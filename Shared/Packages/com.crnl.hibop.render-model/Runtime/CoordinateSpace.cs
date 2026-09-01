using System;

namespace CRNL.HiBoP.RenderModel
{
    public enum CoordinateHandedness : byte
    {
        Unknown = 0,
        Right = 1,
        Left = 2,
    }

    public enum CoordinateAxisOrder : byte
    {
        Unknown = 0,
        Xyz = 1,
    }

    public enum LengthUnit : byte
    {
        Unknown = 0,
        Meter = 1,
        Millimeter = 2,
    }

    public readonly struct CoordinateSpace
    {
        public CoordinateSpace(CoordinateHandedness handedness, CoordinateAxisOrder axisOrder, LengthUnit unit, float metersPerUnit, Matrix4x4F assetToBrain, uint mappingVersion)
        {
            if (handedness == CoordinateHandedness.Unknown)
                throw new ArgumentOutOfRangeException(nameof(handedness));
            if (axisOrder == CoordinateAxisOrder.Unknown)
                throw new ArgumentOutOfRangeException(nameof(axisOrder));
            if (unit == LengthUnit.Unknown)
                throw new ArgumentOutOfRangeException(nameof(unit));
            if (!RenderMath.IsFinite(metersPerUnit) || metersPerUnit <= 0f)
                throw new ArgumentOutOfRangeException(nameof(metersPerUnit));
            if (!assetToBrain.IsFinite)
                throw new ArgumentOutOfRangeException(nameof(assetToBrain));
            if (mappingVersion == 0)
                throw new ArgumentOutOfRangeException(nameof(mappingVersion));

            Handedness = handedness;
            AxisOrder = axisOrder;
            Unit = unit;
            MetersPerUnit = metersPerUnit;
            AssetToBrain = assetToBrain;
            MappingVersion = mappingVersion;
        }

        public static CoordinateSpace DesktopUnityMillimetersV1 { get; } = new(CoordinateHandedness.Left, CoordinateAxisOrder.Xyz, LengthUnit.Millimeter, 0.001f, Matrix4x4F.Identity, 1);

        public CoordinateHandedness Handedness { get; }
        public CoordinateAxisOrder AxisOrder { get; }
        public LengthUnit Unit { get; }
        public float MetersPerUnit { get; }
        public Matrix4x4F AssetToBrain { get; }
        public uint MappingVersion { get; }

        public bool IsValid => Handedness != CoordinateHandedness.Unknown && AxisOrder != CoordinateAxisOrder.Unknown && Unit != LengthUnit.Unknown && RenderMath.IsFinite(MetersPerUnit) && MetersPerUnit > 0f && AssetToBrain.IsFinite && MappingVersion != 0;
    }
}
