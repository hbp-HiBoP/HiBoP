using System;
using System.IO;
using System.Linq;
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

        [TestCase(23.976f)]
        [TestCase(25.0f)]
        [TestCase(29.97f)]
        [Category("NativeMigration")]
        public void VideoStream_WritesExactFpsFrameCountsAndSeekableIndex(float fps)
        {
            string path = Path.Combine(Path.GetTempPath(), $"hibop_video_index_{Guid.NewGuid():N}.avi");
            Texture2D frame = CreateSolidFrame(12, 8, Color.green);
            try
            {
                using (VideoStream stream = new())
                {
                    stream.Open(path, frame.width, frame.height, fps);
                    stream.WriteFrame(frame);
                    stream.WriteFrame(frame);
                    stream.WriteFrame(frame);
                    Assert.That(stream.FrameCount, Is.EqualTo(3));
                }

                byte[] bytes = File.ReadAllBytes(path);
                int streamHeader = FindFourCc(bytes, "strh");
                int aviHeader = FindFourCc(bytes, "avih");
                Assert.That(ReadUInt32(bytes, streamHeader + 28), Is.EqualTo(1000u), "dwScale");
                Assert.That(ReadUInt32(bytes, streamHeader + 32), Is.EqualTo((uint)Math.Round(fps * 1000.0f)), "dwRate");
                Assert.That(ReadUInt32(bytes, streamHeader + 40), Is.EqualTo(3u), "stream frame count");
                Assert.That(ReadUInt32(bytes, aviHeader + 24), Is.EqualTo(3u), "AVI frame count");
                Assert.That(
                    ReadUInt32(bytes, aviHeader + 8),
                    Is.EqualTo((uint)Math.Round(1000000.0 / fps)).Within(1u),
                    "microseconds per frame");

                AssertSeekableIndex(bytes, expectedFrameCount: 3);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(frame);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void VideoStream_PreservesUnityBottomTopOrientationAndFinalizesEndOfStream()
        {
            string path = Path.Combine(Path.GetTempPath(), $"hibop_video_orientation_{Guid.NewGuid():N}.avi");
            Texture2D frame = CreateHorizontalBandsFrame(64, 64);
            try
            {
                using VideoStream stream = new();
                stream.Open(path, frame.width, frame.height, 25.0f);
                stream.WriteFrame(frame);
                stream.Close();

                Assert.That(() => stream.WriteFrame(frame), Throws.InvalidOperationException);
                Assert.That(() => stream.Close(), Throws.Nothing);

                byte[] bytes = File.ReadAllBytes(path);
                Assert.That(ReadUInt32(bytes, 4) + 8u, Is.EqualTo((uint)bytes.Length), "RIFF end-of-stream size");
                (int Offset, int Size) indexedFrame = ReadIndexedFrames(bytes).Single();
                Texture2D decoded = DecodeJpeg(bytes, indexedFrame.Offset, indexedFrame.Size);
                try
                {
                    Color32 bottom = decoded.GetPixel(decoded.width / 2, decoded.height / 4);
                    Color32 top = decoded.GetPixel(decoded.width / 2, 3 * decoded.height / 4);
                    Assert.That(bottom.r, Is.GreaterThan(200), "bottom band red");
                    Assert.That(bottom.b, Is.LessThan(60), "bottom band red");
                    Assert.That(top.b, Is.GreaterThan(200), "top band blue");
                    Assert.That(top.r, Is.LessThan(60), "top band blue");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(decoded);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(frame);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void VideoStream_WritesAndDecodesA1080pFrame()
        {
            string path = Path.Combine(Path.GetTempPath(), $"hibop_video_1080p_{Guid.NewGuid():N}.avi");
            Texture2D frame = CreateSolidFrame(1920, 1080, new Color(0.2f, 0.6f, 0.9f, 1.0f));
            try
            {
                using (VideoStream stream = new())
                {
                    stream.Open(path, frame.width, frame.height, 30.0f);
                    stream.WriteFrame(frame);
                }

                byte[] bytes = File.ReadAllBytes(path);
                int streamFormat = FindFourCc(bytes, "strf");
                Assert.That(ReadInt32(bytes, streamFormat + 12), Is.EqualTo(1920));
                Assert.That(ReadInt32(bytes, streamFormat + 16), Is.EqualTo(1080));
                (int Offset, int Size) indexedFrame = ReadIndexedFrames(bytes).Single();
                Texture2D decoded = DecodeJpeg(bytes, indexedFrame.Offset, indexedFrame.Size);
                try
                {
                    Assert.That(decoded.width, Is.EqualTo(1920));
                    Assert.That(decoded.height, Is.EqualTo(1080));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(decoded);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(frame);
                if (File.Exists(path)) File.Delete(path);
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

        private static Texture2D CreateHorizontalBandsFrame(int width, int height)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, mipChain: false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; ++y)
            {
                Color32 color = y < height / 2 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 255);
                for (int x = 0; x < width; ++x)
                {
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);
            return texture;
        }

        private static void AssertSeekableIndex(byte[] bytes, int expectedFrameCount)
        {
            (int Offset, int Size)[] frames = ReadIndexedFrames(bytes);
            Assert.That(frames, Has.Length.EqualTo(expectedFrameCount));
            foreach ((int offset, int size) in frames)
            {
                Assert.That(ReadFourCc(bytes, offset - 8), Is.EqualTo("00dc"));
                Assert.That(ReadInt32(bytes, offset - 4), Is.EqualTo(size));
                Assert.That(bytes[offset], Is.EqualTo(0xFF));
                Assert.That(bytes[offset + 1], Is.EqualTo(0xD8));
                Assert.That(bytes[offset + size - 2], Is.EqualTo(0xFF));
                Assert.That(bytes[offset + size - 1], Is.EqualTo(0xD9));
            }
        }

        private static (int Offset, int Size)[] ReadIndexedFrames(byte[] bytes)
        {
            int movi = FindFourCc(bytes, "movi");
            int index = FindFourCc(bytes, "idx1");
            Assert.That(movi, Is.GreaterThanOrEqualTo(0));
            Assert.That(index, Is.GreaterThan(movi));
            int indexSize = ReadInt32(bytes, index + 4);
            Assert.That(indexSize % 16, Is.EqualTo(0));
            int entryCount = indexSize / 16;
            (int Offset, int Size)[] frames = new (int, int)[entryCount];
            for (int entry = 0; entry < entryCount; ++entry)
            {
                int entryOffset = index + 8 + entry * 16;
                Assert.That(ReadFourCc(bytes, entryOffset), Is.EqualTo("00dc"));
                Assert.That(ReadUInt32(bytes, entryOffset + 4), Is.EqualTo(0x10u), "key-frame flag");
                int chunkStart = checked(movi + 4 + (int)ReadUInt32(bytes, entryOffset + 8));
                int size = checked((int)ReadUInt32(bytes, entryOffset + 12));
                frames[entry] = (chunkStart + 8, size);
            }
            Assert.That(index + 8 + indexSize, Is.EqualTo(bytes.Length), "idx1 must terminate the stream");
            return frames;
        }

        private static Texture2D DecodeJpeg(byte[] bytes, int offset, int size)
        {
            byte[] jpeg = new byte[size];
            Array.Copy(bytes, offset, jpeg, 0, size);
            Texture2D decoded = new(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!ImageConversion.LoadImage(decoded, jpeg))
            {
                UnityEngine.Object.DestroyImmediate(decoded);
                Assert.Fail("Unity failed to decode an indexed MJPEG frame.");
            }
            return decoded;
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

        private static uint ReadUInt32(byte[] bytes, int offset) => BitConverter.ToUInt32(bytes, offset);
    }
}
