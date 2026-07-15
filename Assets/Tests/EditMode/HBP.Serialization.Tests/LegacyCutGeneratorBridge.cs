using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL;

namespace HBP.Tests.Serialization
{
    internal static class LegacyCutGeneratorBridge
    {
        public static void FillTextureWithVolume(CutGenerator generator, LegacyTextureBridge colorScheme, float calMin, float calMax)
        {
            fill_texture_with_volume_CutGenerator(generator.getHandle().Handle, colorScheme.Handle, calMin, calMax);
        }

        public static void UpdateTextureWithVolume(CutGenerator generator, LegacyTextureBridge texture)
        {
            update_texture_with_volume_CutGenerator(generator.getHandle().Handle, texture.Handle);
        }

        public static void FillTextureWithActivity(CutGenerator generator, LegacyTextureBridge colorScheme, int timelineIndex, float alpha)
        {
            fill_texture_with_activity_CutGenerator(generator.getHandle().Handle, colorScheme.Handle, timelineIndex, alpha);
        }

        public static void UpdateTextureWithActivity(CutGenerator generator, LegacyTextureBridge texture)
        {
            update_texture_with_activity_CutGenerator(generator.getHandle().Handle, texture.Handle);
        }

        [DllImport("hbp_export", EntryPoint = "fill_texture_with_volume_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        private static extern void fill_texture_with_volume_CutGenerator(IntPtr generator, IntPtr colorScheme, float calMin, float calMax);

        [DllImport("hbp_export", EntryPoint = "update_texture_with_volume_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        private static extern void update_texture_with_volume_CutGenerator(IntPtr generator, IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "fill_texture_with_activity_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        private static extern void fill_texture_with_activity_CutGenerator(IntPtr generator, IntPtr colorScheme, int timelineIndex, float alpha);

        [DllImport("hbp_export", EntryPoint = "update_texture_with_activity_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        private static extern void update_texture_with_activity_CutGenerator(IntPtr generator, IntPtr texture);
    }
}
