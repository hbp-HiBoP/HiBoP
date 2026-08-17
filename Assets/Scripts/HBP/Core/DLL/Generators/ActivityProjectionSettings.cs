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

        public static event Action OnChanged;

        public static int VolumeGridDimension
        {
            get => s_VolumeGridDimension;
            set
            {
                if (value < 2) throw new ArgumentOutOfRangeException(nameof(value));
                if (s_VolumeGridDimension != value)
                {
                    s_VolumeGridDimension = value;
                    OnChanged?.Invoke();
                }
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

                if (s_VolumeInterpolation != value)
                {
                    s_VolumeInterpolation = value;
                    OnChanged?.Invoke();
                }
            }
        }

        public static void ResetDefaults()
        {
            bool changed = s_VolumeGridDimension != DefaultVolumeGridDimension || s_VolumeInterpolation != DefaultVolumeInterpolation;
            s_VolumeGridDimension = DefaultVolumeGridDimension;
            s_VolumeInterpolation = DefaultVolumeInterpolation;
            if (changed) OnChanged?.Invoke();
        }
    }
}
