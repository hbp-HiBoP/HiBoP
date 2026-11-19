using HBP.Core.DLL;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class LocalizerRenderingTests
    {
        [Test, Category("NativeDll"), Category("HbpCoreOnly")]
        public void WithoutMask_ZeroAndBelowCalibrationRemainVisible()
        {
            NativeParityAssert.RequireHbpCore();
            using Volume volume = new();
            float[] values = { -10, 0, 79, 80, 120, 150 };
            Color32[] palette = { new(0, 0, 255, 255), new(255, 0, 0, 255) };
            Color[] colors = volume.ConvertValuesToColors(values, null, 80, 100, 120, palette);
            Assert.That(colors, Is.EqualTo(new[] { Color.blue, Color.blue, Color.blue, Color.blue, Color.red, Color.red }));
        }

        [Test, Category("NativeDll"), Category("HbpCoreOnly")]
        public void ExplicitMask_StillHidesExcludedValues()
        {
            NativeParityAssert.RequireHbpCore();
            using Volume volume = new();
            float[] values = { 0, 120, 0, 120 };
            int[] mask = { 1, 0, 0, 1 };
            Color32[] palette = { new(0, 0, 255, 255), new(255, 0, 0, 255) };
            Color[] colors = volume.ConvertValuesToColors(values, mask, 80, 100, 120, palette);
            Assert.That(colors, Is.EqualTo(new[] { Color.blue, Color.clear, Color.clear, Color.red }));
        }
    }
}
