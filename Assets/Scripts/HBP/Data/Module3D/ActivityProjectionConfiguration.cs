using System;
using HBP.Core.DLL;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Applies the 3D module's initial activity projection settings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActivityProjectionConfiguration : MonoBehaviour
    {
        [SerializeField, Min(2)] private int m_VolumeGridDimension = ActivityProjectionSettings.DefaultVolumeGridDimension;
        [SerializeField] private VolumeInterpolation m_VolumeInterpolation = ActivityProjectionSettings.DefaultVolumeInterpolation;

        public int VolumeGridDimension => m_VolumeGridDimension;
        public VolumeInterpolation VolumeInterpolation => m_VolumeInterpolation;

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        /// <summary>
        /// Explicitly applies the serialized configuration, replacing any session overrides.
        /// </summary>
        public void Apply()
        {
            ValidateSettings();
            ActivityProjectionSettings.VolumeGridDimension = m_VolumeGridDimension;
            ActivityProjectionSettings.VolumeInterpolation = m_VolumeInterpolation;
        }

        private void ValidateSettings()
        {
            if (m_VolumeGridDimension < 2)
            {
                m_VolumeGridDimension = ActivityProjectionSettings.DefaultVolumeGridDimension;
            }

            if (!Enum.IsDefined(typeof(VolumeInterpolation), m_VolumeInterpolation))
            {
                m_VolumeInterpolation = ActivityProjectionSettings.DefaultVolumeInterpolation;
            }
        }
    }
}
