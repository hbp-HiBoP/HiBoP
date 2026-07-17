using System;
using HBP.Core.Enums;

namespace HBP.Core.DLL
{
    public static class ActivityProjectionSettings
    {
        public const int DefaultVolumeGridDimension = 80;
        public const VolumeInterpolation DefaultVolumeInterpolation = VolumeInterpolation.Trilinear;

        private static int s_VolumeGridDimension = DefaultVolumeGridDimension;
        private static VolumeInterpolation s_VolumeInterpolation = DefaultVolumeInterpolation;

        public static int VolumeGridDimension
        {
            get => s_VolumeGridDimension;
            set
            {
                if (value < 2) throw new ArgumentOutOfRangeException(nameof(value));
                s_VolumeGridDimension = value;
            }
        }

        public static VolumeInterpolation VolumeInterpolation
        {
            get => s_VolumeInterpolation;
            set
            {
                if (!Enum.IsDefined(typeof(VolumeInterpolation), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                s_VolumeInterpolation = value;
            }
        }

        public static void ResetDefaults()
        {
            s_VolumeGridDimension = DefaultVolumeGridDimension;
            s_VolumeInterpolation = DefaultVolumeInterpolation;
        }
    }
}
