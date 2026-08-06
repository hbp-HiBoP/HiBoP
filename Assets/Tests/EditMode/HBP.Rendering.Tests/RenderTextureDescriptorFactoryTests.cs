using System;
using HBP.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HBP.Tests.Rendering
{
    public class RenderTextureDescriptorFactoryTests
    {
        [Test]
        public void ViewDescriptor_HasTheMigrationColorDepthAndSamplingContract()
        {
            RenderTextureDescriptor descriptor = HBPRenderTextureDescriptorFactory.CreateViewDescriptor(2048, 1024);

            Assert.That(descriptor.width, Is.EqualTo(2048));
            Assert.That(descriptor.height, Is.EqualTo(1024));
            Assert.That(descriptor.graphicsFormat, Is.EqualTo(GraphicsFormat.R8G8B8A8_SRGB));
            Assert.That(descriptor.depthStencilFormat, Is.EqualTo(GraphicsFormat.D24_UNorm_S8_UInt));
            Assert.That(descriptor.sRGB, Is.True);
            Assert.That(descriptor.msaaSamples, Is.EqualTo(1));
            Assert.That(descriptor.dimension, Is.EqualTo(TextureDimension.Tex2D));
            Assert.That(descriptor.volumeDepth, Is.EqualTo(1));
            Assert.That(descriptor.useMipMap, Is.False);
            Assert.That(descriptor.autoGenerateMips, Is.False);
            Assert.That(descriptor.enableRandomWrite, Is.False);
            Assert.That(descriptor.useDynamicScale, Is.False);
            Assert.That(descriptor.memoryless, Is.EqualTo(RenderTextureMemoryless.None));
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        public void ViewDescriptor_RejectsInvalidDimensions(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HBPRenderTextureDescriptorFactory.CreateViewDescriptor(width, height));
        }
    }
}
