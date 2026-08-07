using System;
using UnityEngine;

namespace HBP.Rendering
{
    public sealed class HBPRenderTextureOwner : IDisposable
    {
        private RenderTexture m_Texture;

        public RenderTexture Texture => m_Texture;

        public int AllocationCount { get; private set; }

        public RenderTexture Acquire(int width, int height, string name)
        {
            RenderTextureDescriptor descriptor = HBPRenderTextureDescriptorFactory.CreateViewDescriptor(width, height);
            if (m_Texture != null && Matches(m_Texture.descriptor, descriptor))
                return m_Texture;

            Release();
            m_Texture = new RenderTexture(descriptor)
            {
                name = name,
                hideFlags = HideFlags.DontSave
            };
            m_Texture.Create();
            ++AllocationCount;
            return m_Texture;
        }

        public void Release()
        {
            if (m_Texture == null)
                return;

            m_Texture.Release();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(m_Texture);
            else
                UnityEngine.Object.DestroyImmediate(m_Texture);
            m_Texture = null;
        }

        public void Dispose()
        {
            Release();
        }

        private static bool Matches(RenderTextureDescriptor current, RenderTextureDescriptor requested)
        {
            return current.width == requested.width && current.height == requested.height && current.graphicsFormat == requested.graphicsFormat && current.depthStencilFormat == requested.depthStencilFormat && current.msaaSamples == requested.msaaSamples && current.dimension == requested.dimension && current.sRGB == requested.sRGB && current.useMipMap == requested.useMipMap && current.enableRandomWrite == requested.enableRandomWrite;
        }
    }
}
