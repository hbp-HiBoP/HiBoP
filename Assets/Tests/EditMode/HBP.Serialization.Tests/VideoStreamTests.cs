using System;
using System.IO;
using System.Text;
using HBP.Core.DLL;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class VideoStreamTests
    {
        [Test]
        [Category("NativeMigration")]
        public void VideoStream_WritesAviFromUnityTextureWithoutNativeEncoder()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"hibop_video_stream_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "stream.avi");
                Texture2D firstFrame = CreateSolidFrame(16, 16, Color.red);
                Texture2D secondFrame = CreateSolidFrame(16, 16, Color.blue);
                try
                {
                    using VideoStream stream = new();
                    {
                        stream.Open(path, firstFrame.width, firstFrame.height, 25.0f);
                        stream.WriteFrame(firstFrame);
                        stream.WriteFrame(secondFrame);
                        Assert.That(stream.FrameCount, Is.EqualTo(2));
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(firstFrame);
                    UnityEngine.Object.DestroyImmediate(secondFrame);
                }

                byte[] bytes = File.ReadAllBytes(path);
                Assert.That(ReadFourCc(bytes, 0), Is.EqualTo("RIFF"));
                Assert.That(ReadFourCc(bytes, 8), Is.EqualTo("AVI "));
                Assert.That(FindFourCc(bytes, "hbp_export"), Is.EqualTo(-1));

                int streamHeaderPosition = FindFourCc(bytes, "strh");
                Assert.That(streamHeaderPosition, Is.GreaterThanOrEqualTo(0));
                Assert.That(ReadInt32(bytes, streamHeaderPosition + 4), Is.EqualTo(64));
                Assert.That(ReadFourCc(bytes, streamHeaderPosition + 12), Is.EqualTo("MJPG"));

                int streamFormatPosition = FindFourCc(bytes, "strf");
                Assert.That(streamFormatPosition, Is.GreaterThanOrEqualTo(0));
                Assert.That(ReadInt32(bytes, streamFormatPosition + 4), Is.EqualTo(40));
                Assert.That(ReadInt32(bytes, streamFormatPosition + 12), Is.EqualTo(16));
                Assert.That(ReadInt32(bytes, streamFormatPosition + 16), Is.EqualTo(16));
                Assert.That(ReadInt16(bytes, streamFormatPosition + 22), Is.EqualTo(24));
                Assert.That(ReadFourCc(bytes, streamFormatPosition + 24), Is.EqualTo("MJPG"));

                int firstFramePosition = FindFourCc(bytes, "00dc");
                Assert.That(firstFramePosition, Is.GreaterThanOrEqualTo(0));
                int firstFrameSize = ReadInt32(bytes, firstFramePosition + 4);
                Assert.That(firstFrameSize, Is.GreaterThan(0));
                Assert.That(bytes[firstFramePosition + 8], Is.EqualTo(0xFF), "JPEG SOI marker byte 0");
                Assert.That(bytes[firstFramePosition + 9], Is.EqualTo(0xD8), "JPEG SOI marker byte 1");
                AssertFirstJpegFrameIsRed(bytes, firstFramePosition + 8, firstFrameSize);
                Assert.That(FindFourCc(bytes, "idx1"), Is.GreaterThan(firstFramePosition));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        private static Texture2D CreateSolidFrame(int width, int height, Color color)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, mipChain: false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false);
            return texture;
        }

        private static void AssertFirstJpegFrameIsRed(byte[] bytes, int jpegOffset, int jpegSize)
        {
            byte[] jpegBytes = new byte[jpegSize];
            Array.Copy(bytes, jpegOffset, jpegBytes, 0, jpegBytes.Length);
            Texture2D decoded = new(2, 2, TextureFormat.RGBA32, mipChain: false);
            try
            {
                Assert.That(ImageConversion.LoadImage(decoded, jpegBytes), Is.True);
                Color32 center = decoded.GetPixel(decoded.width / 2, decoded.height / 2);
                Assert.That(center.r, Is.GreaterThan(200));
                Assert.That(center.g, Is.LessThan(80));
                Assert.That(center.b, Is.LessThan(80));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }

        private static int FindFourCc(byte[] bytes, string fourCc)
        {
            byte[] pattern = Encoding.ASCII.GetBytes(fourCc);
            for (int i = 0; i <= bytes.Length - pattern.Length; ++i)
            {
                bool matches = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (bytes[i + j] != pattern[j])
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string ReadFourCc(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 4);

        private static short ReadInt16(byte[] bytes, int offset) => BitConverter.ToInt16(bytes, offset);

        private static int ReadInt32(byte[] bytes, int offset) => BitConverter.ToInt32(bytes, offset);
    }
}
