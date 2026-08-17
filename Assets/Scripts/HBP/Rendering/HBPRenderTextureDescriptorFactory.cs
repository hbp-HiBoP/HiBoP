using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HBP.Rendering
{
    public static class HBPRenderTextureDescriptorFactory
    {
        public static RenderTextureDescriptor CreateViewDescriptor(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            return new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB,
                depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt,
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                msaaSamples = 1,
                sRGB = true,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false,
                bindMS = false,
                useDynamicScale = false,
                memoryless = RenderTextureMemoryless.None,
            };
        }
    }
}
