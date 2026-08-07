using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace HBP.Core.DLL
{
    public sealed class VideoStream : IDisposable
    {
        private const uint AviHasIndex = 0x10;
        private const uint KeyFrame = 0x10;
        private const uint BitmapInfoHeaderSize = 40;
        private const ushort BitmapPlanes = 1;
        private const ushort BitsPerPixel = 24;
        private const int JpegQuality = 95;

        private readonly List<AviIndexEntry> m_IndexEntries = new();
        private FileStream m_Stream;
        private BinaryWriter m_Writer;
        private long m_RiffSizePosition;
        private long m_AviHeaderFrameCountPosition;
        private long m_AviHeaderSuggestedBufferSizePosition;
        private long m_StreamHeaderFrameCountPosition;
        private long m_StreamHeaderSuggestedBufferSizePosition;
        private long m_StreamFormatImageSizePosition;
        private long m_MoviListSizePosition;
        private long m_MoviDataStartPosition;
        private int m_Width;
        private int m_Height;
        private int m_SuggestedBufferSize;
        private int m_MaxFrameByteSize;
        private uint m_FrameRateScale;
        private uint m_FrameRateRate;
        private bool m_IsOpen;
        private bool m_IsDisposed;
        private byte[] m_FrameBuffer;

        public int FrameCount { get; private set; }

        public void Open(string path, int width, int height, float fps)
        {
            if (m_IsOpen)
            {
                throw new InvalidOperationException("The video stream is already open.");
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The video path is empty.", nameof(path));
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (fps <= 0.0f || float.IsNaN(fps) || float.IsInfinity(fps))
            {
                throw new ArgumentOutOfRangeException(nameof(fps));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            m_Width = width;
            m_Height = height;
            m_SuggestedBufferSize = width * height * 3;
            m_MaxFrameByteSize = m_SuggestedBufferSize;
            m_IndexEntries.Clear();
            FrameCount = 0;
            SetFrameRate(fps);

            m_Stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            m_Writer = new BinaryWriter(m_Stream);
            WriteHeader();
            m_IsOpen = true;
        }

        public void WriteFrame(Texture2D texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (!m_IsOpen)
            {
                throw new InvalidOperationException("The video stream is not open.");
            }

            if (texture.width != m_Width || texture.height != m_Height)
            {
                throw new ArgumentException($"The frame size must be {m_Width}x{m_Height}.", nameof(texture));
            }

            NativeArray<byte> rawTextureData = texture.GetRawTextureData<byte>();
            using NativeArray<byte> encodedFrame = ImageConversion.EncodeNativeArrayToJPG(rawTextureData, texture.graphicsFormat, (uint)m_Width, (uint)m_Height, 0, JpegQuality);
            int frameByteCount = encodedFrame.Length;
            if (frameByteCount == 0)
            {
                throw new InvalidOperationException("Failed to encode the video frame as JPEG.");
            }

            if (m_FrameBuffer == null || m_FrameBuffer.Length < frameByteCount)
            {
                m_FrameBuffer = new byte[frameByteCount];
            }

            NativeArray<byte>.Copy(encodedFrame, 0, m_FrameBuffer, 0, frameByteCount);
            m_MaxFrameByteSize = Math.Max(m_MaxFrameByteSize, frameByteCount);

            long chunkStartPosition = m_Stream.Position;
            WriteFourCc("00dc");
            m_Writer.Write((uint)frameByteCount);
            m_Writer.Write(m_FrameBuffer, 0, frameByteCount);

            if ((frameByteCount & 1) != 0)
            {
                m_Writer.Write((byte)0);
            }

            m_IndexEntries.Add(new AviIndexEntry(chunkStartPosition - m_MoviDataStartPosition, (uint)frameByteCount));
            ++FrameCount;
        }

        public void Close()
        {
            if (!m_IsOpen)
            {
                return;
            }

            m_Writer.Flush();
            long beforeIndexPosition = m_Stream.Position;
            PatchUInt32(m_MoviListSizePosition, checked((uint)(beforeIndexPosition - (m_MoviListSizePosition + 4))));
            WriteIndex();
            PatchUInt32(m_AviHeaderFrameCountPosition, checked((uint)FrameCount));
            PatchUInt32(m_AviHeaderSuggestedBufferSizePosition, checked((uint)m_MaxFrameByteSize));
            PatchUInt32(m_StreamHeaderFrameCountPosition, checked((uint)FrameCount));
            PatchUInt32(m_StreamHeaderSuggestedBufferSizePosition, checked((uint)m_MaxFrameByteSize));
            PatchUInt32(m_StreamFormatImageSizePosition, checked((uint)m_MaxFrameByteSize));
            PatchUInt32(m_RiffSizePosition, checked((uint)(m_Stream.Length - 8)));
            m_Writer.Flush();

            m_Writer.Dispose();
            m_Stream.Dispose();
            m_Writer = null;
            m_Stream = null;
            m_IsOpen = false;
        }

        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            Close();
            m_IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        private void WriteHeader()
        {
            WriteFourCc("RIFF");
            m_RiffSizePosition = m_Stream.Position;
            m_Writer.Write((uint)0);
            WriteFourCc("AVI ");

            long hdrlSizePosition = StartList("hdrl");
            WriteAviHeaderChunk();

            long strlSizePosition = StartList("strl");
            WriteStreamHeaderChunk();
            WriteStreamFormatChunk();
            EndChunkOrList(strlSizePosition);
            EndChunkOrList(hdrlSizePosition);

            m_MoviListSizePosition = StartList("movi");
            m_MoviDataStartPosition = m_Stream.Position;
        }

        private void WriteAviHeaderChunk()
        {
            long chunkSizePosition = StartChunk("avih");
            m_Writer.Write((uint)Math.Round(1000000.0 / ((double)m_FrameRateRate / m_FrameRateScale)));
            m_Writer.Write((uint)Math.Ceiling(m_SuggestedBufferSize * ((double)m_FrameRateRate / m_FrameRateScale)));
            m_Writer.Write((uint)0);
            m_Writer.Write(AviHasIndex);
            m_AviHeaderFrameCountPosition = m_Stream.Position;
            m_Writer.Write((uint)0);
            m_Writer.Write((uint)0);
            m_Writer.Write((uint)1);
            m_AviHeaderSuggestedBufferSizePosition = m_Stream.Position;
            m_Writer.Write((uint)m_SuggestedBufferSize);
            m_Writer.Write((uint)m_Width);
            m_Writer.Write((uint)m_Height);
            for (int i = 0; i < 4; ++i)
            {
                m_Writer.Write((uint)0);
            }

            EndChunkOrList(chunkSizePosition);
        }

        private void WriteStreamHeaderChunk()
        {
            long chunkSizePosition = StartChunk("strh");
            WriteFourCc("vids");
            WriteFourCc("MJPG");
            m_Writer.Write((uint)0);
            m_Writer.Write((ushort)0);
            m_Writer.Write((ushort)0);
            m_Writer.Write((uint)0);
            m_Writer.Write(m_FrameRateScale);
            m_Writer.Write(m_FrameRateRate);
            m_Writer.Write((uint)0);
            m_StreamHeaderFrameCountPosition = m_Stream.Position;
            m_Writer.Write((uint)0);
            m_StreamHeaderSuggestedBufferSizePosition = m_Stream.Position;
            m_Writer.Write((uint)m_SuggestedBufferSize);
            m_Writer.Write(uint.MaxValue);
            m_Writer.Write((uint)0);
            m_Writer.Write(0);
            m_Writer.Write(0);
            m_Writer.Write(m_Width);
            m_Writer.Write(m_Height);
            EndChunkOrList(chunkSizePosition);
        }

        private void WriteStreamFormatChunk()
        {
            long chunkSizePosition = StartChunk("strf");
            m_Writer.Write(BitmapInfoHeaderSize);
            m_Writer.Write(m_Width);
            m_Writer.Write(m_Height);
            m_Writer.Write(BitmapPlanes);
            m_Writer.Write(BitsPerPixel);
            WriteFourCc("MJPG");
            m_StreamFormatImageSizePosition = m_Stream.Position;
            m_Writer.Write((uint)m_SuggestedBufferSize);
            m_Writer.Write(0);
            m_Writer.Write(0);
            m_Writer.Write((uint)0);
            m_Writer.Write((uint)0);
            EndChunkOrList(chunkSizePosition);
        }

        private void WriteIndex()
        {
            long chunkSizePosition = StartChunk("idx1");
            foreach (AviIndexEntry entry in m_IndexEntries)
            {
                WriteFourCc("00dc");
                m_Writer.Write(KeyFrame);
                m_Writer.Write(checked((uint)entry.Offset));
                m_Writer.Write(entry.Size);
            }

            EndChunkOrList(chunkSizePosition);
        }

        private long StartList(string listType)
        {
            WriteFourCc("LIST");
            long sizePosition = m_Stream.Position;
            m_Writer.Write((uint)0);
            WriteFourCc(listType);
            return sizePosition;
        }

        private long StartChunk(string chunkId)
        {
            WriteFourCc(chunkId);
            long sizePosition = m_Stream.Position;
            m_Writer.Write((uint)0);
            return sizePosition;
        }

        private void EndChunkOrList(long sizePosition)
        {
            long currentPosition = m_Stream.Position;
            long dataSize = currentPosition - (sizePosition + 4);
            PatchUInt32(sizePosition, checked((uint)dataSize));
            m_Stream.Position = currentPosition;
            if ((dataSize & 1) != 0)
            {
                m_Writer.Write((byte)0);
            }
        }

        private void PatchUInt32(long position, uint value)
        {
            long currentPosition = m_Stream.Position;
            m_Stream.Position = position;
            m_Writer.Write(value);
            m_Stream.Position = currentPosition;
        }

        private void WriteFourCc(string value)
        {
            if (value.Length != 4)
            {
                throw new ArgumentException("A FourCC must contain exactly four characters.", nameof(value));
            }

            for (int i = 0; i < value.Length; ++i)
            {
                m_Writer.Write((byte)value[i]);
            }
        }

        private void SetFrameRate(float fps)
        {
            m_FrameRateScale = 1000;
            m_FrameRateRate = Math.Max(1u, (uint)Math.Round(fps * m_FrameRateScale));
        }

        private readonly struct AviIndexEntry
        {
            public AviIndexEntry(long offset, uint size)
            {
                Offset = offset;
                Size = size;
            }

            public long Offset { get; }
            public uint Size { get; }
        }
    }
}
