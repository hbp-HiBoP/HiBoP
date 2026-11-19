using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.PlayMode.Module3D
{
    public class ActivityProjectionConfigurationPlayModeTests
    {
        [Test]
        public async Task Awake_AppliesConfigurationOnceAndReactivationPreservesSessionSettings()
        {
            int previousDimension = ActivityProjectionSettings.VolumeGridDimension;
            VolumeInterpolation previousInterpolation = ActivityProjectionSettings.VolumeInterpolation;
            GameObject root = new("Activity projection startup test");
            root.SetActive(false);
            try
            {
                ActivityProjectionConfiguration configuration = root.AddComponent<ActivityProjectionConfiguration>();
                typeof(ActivityProjectionConfiguration).GetField("m_VolumeGridDimension", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(configuration, 96);
                typeof(ActivityProjectionConfiguration).GetField("m_VolumeInterpolation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(configuration, VolumeInterpolation.Nearest);
                ActivityProjectionSettings.ResetDefaults();

                root.SetActive(true);
                await UniTask.NextFrame();

                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(96));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Nearest));

                ActivityProjectionSettings.VolumeGridDimension = 112;
                ActivityProjectionSettings.VolumeInterpolation = VolumeInterpolation.Trilinear;
                root.SetActive(false);
                root.SetActive(true);
                await UniTask.NextFrame();

                Assert.That(ActivityProjectionSettings.VolumeGridDimension, Is.EqualTo(112));
                Assert.That(ActivityProjectionSettings.VolumeInterpolation, Is.EqualTo(VolumeInterpolation.Trilinear));
            }
            finally
            {
                Object.Destroy(root);
                await UniTask.NextFrame();
                ActivityProjectionSettings.VolumeGridDimension = previousDimension;
                ActivityProjectionSettings.VolumeInterpolation = previousInterpolation;
            }
        }
    }
}
