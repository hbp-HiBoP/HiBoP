using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HBP.Tests.Serialization
{
    internal sealed class NativePerformanceBenchmarkFixtures
    {
        private readonly string m_GiftiSource;
        private readonly string m_RealSurfaceObjSource;

        public NativePerformanceBenchmarkFixtures(string root)
        {
            Root = Path.GetFullPath(root);
            SmallNifti = Path.Combine(Root, "volume-32x32x32.nii");
            LargeNifti = Path.Combine(Root, "volume-64x64x64.nii");
            MultiNifti = Path.Combine(Root, "volume-48x48x48x4.nii");
            AtlasNifti = Path.Combine(Root, "atlas-32x32x32.nii");
            Grid64Obj = Path.Combine(Root, "grid-64x64.obj");
            Grid64OffsetObj = Path.Combine(Root, "grid-64x64-offset.obj");
            Grid128Obj = Path.Combine(Root, "grid-128x128.obj");
            CubeObj = Path.Combine(Root, "cube.obj");
            MarsIndex = Path.Combine(Root, "mars-index.csv");
            Brodmann = Path.Combine(Root, "brodmann.txt");
            Gifti = Path.Combine(Root, "MNI-Lhemi.gii");
            RealSurfaceObj = Path.Combine(Root, "MNI-Lhemi.obj");
            m_GiftiSource = Path.Combine(UnityEngine.Application.dataPath, "Data", "Meshes", "MNI_Lhemi.gii");
            m_RealSurfaceObjSource = Path.Combine(UnityEngine.Application.dataPath, "Data", "Meshes", "MNI_single_hight_Lhemi.obj");
        }

        public string Root { get; }
        public string SmallNifti { get; }
        public string LargeNifti { get; }
        public string MultiNifti { get; }
        public string AtlasNifti { get; }
        public string Grid64Obj { get; }
        public string Grid64OffsetObj { get; }
        public string Grid128Obj { get; }
        public string CubeObj { get; }
        public string MarsIndex { get; }
        public string Brodmann { get; }
        public string Gifti { get; }
        public string RealSurfaceObj { get; }

        public void Ensure()
        {
            Directory.CreateDirectory(Root);
            EnsureNifti(SmallNifti, 32, 32, 32, 1, atlas: false);
            EnsureNifti(LargeNifti, 64, 64, 64, 1, atlas: false);
            EnsureNifti(MultiNifti, 48, 48, 48, 4, atlas: false);
            EnsureNifti(AtlasNifti, 32, 32, 32, 1, atlas: true);
            EnsureGridObj(Grid64Obj, 64, 0.0f);
            EnsureGridObj(Grid64OffsetObj, 64, 96.0f);
            EnsureGridObj(Grid128Obj, 128, 0.0f);
            EnsureCubeObj();
            EnsureAtlasMetadata();
            EnsureCopiedFixture(m_GiftiSource, Gifti, "GIFTI");
            EnsureCopiedFixture(m_RealSurfaceObjSource, RealSurfaceObj, "OBJ");
        }

        private static void EnsureCopiedFixture(string source, string destination, string description)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"{description} benchmark source fixture is missing.", source);
            }

            if (!File.Exists(destination) || new FileInfo(destination).Length != new FileInfo(source).Length)
            {
                File.Copy(source, destination, overwrite: true);
            }
        }

        private static void EnsureNifti(string path, int nx, int ny, int nz, int nt, bool atlas)
        {
            long expectedLength = 352L + (long)nx * ny * nz * nt * sizeof(float);
            if (File.Exists(path) && new FileInfo(path).Length == expectedLength)
            {
                return;
            }

            byte[] header = new byte[352];
            WriteInt32(header, 0, 348);
            WriteInt16(header, 40, (short)(nt > 1 ? 4 : 3));
            WriteInt16(header, 42, (short)nx);
            WriteInt16(header, 44, (short)ny);
            WriteInt16(header, 46, (short)nz);
            WriteInt16(header, 48, (short)nt);
            WriteInt16(header, 50, 1);
            WriteInt16(header, 52, 1);
            WriteInt16(header, 54, 1);
            WriteInt16(header, 70, 16);
            WriteInt16(header, 72, 32);
            WriteSingle(header, 76, 1.0f);
            WriteSingle(header, 80, 1.0f);
            WriteSingle(header, 84, 1.0f);
            WriteSingle(header, 88, 1.0f);
            WriteSingle(header, 92, 1.0f);
            WriteSingle(header, 108, 352.0f);
            WriteSingle(header, 112, 1.0f);
            WriteInt16(header, 252, 1);
            WriteInt16(header, 254, 1);
            WriteSingle(header, 280, 1.0f);
            WriteSingle(header, 300, 1.0f);
            WriteSingle(header, 320, 1.0f);
            header[344] = (byte)'n';
            header[345] = (byte)'+';
            header[346] = (byte)'1';

            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            using FileStream stream = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            using BinaryWriter writer = new(stream);
            writer.Write(header);
            for (int t = 0; t < nt; ++t)
            {
                for (int z = 0; z < nz; ++z)
                {
                    for (int y = 0; y < ny; ++y)
                    {
                        for (int x = 0; x < nx; ++x)
                        {
                            float value = atlas ? ((x + 3 * y + 7 * z) % 19 == 0 ? 0.0f : 1.0f + (x + 3 * y + 7 * z) % 124) : ((x * 13 + y * 7 + z * 3 + t * 11) % 257 - 128) / 64.0f;
                            min = Math.Min(min, value);
                            max = Math.Max(max, value);
                            writer.Write(value);
                        }
                    }
                }
            }

            stream.Position = 124;
            writer.Write(max);
            writer.Write(min);
        }

        private static void EnsureGridObj(string path, int size, float xOffset)
        {
            int expectedFaces = (size - 1) * (size - 1) * 2;
            if (File.Exists(path) && File.ReadLines(path).CountLinesStartingWith("f ") == expectedFaces)
            {
                return;
            }

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            writer.WriteLine("# deterministic HiBoP performance grid");
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    float px = xOffset + 63.0f * x / (size - 1);
                    float py = 63.0f * y / (size - 1);
                    float pz = 31.5f + 2.0f * (float)Math.Sin(x * 0.11) * (float)Math.Cos(y * 0.07);
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "v {0:R} {1:R} {2:R}", px, py, pz));
                }
            }

            for (int y = 0; y < size - 1; ++y)
            {
                for (int x = 0; x < size - 1; ++x)
                {
                    int a = y * size + x + 1;
                    int b = a + 1;
                    int c = a + size;
                    int d = c + 1;
                    writer.WriteLine($"f {a} {c} {b}");
                    writer.WriteLine($"f {b} {c} {d}");
                }
            }
        }

        private void EnsureCubeObj()
        {
            if (File.Exists(CubeObj))
            {
                return;
            }

            File.WriteAllText(CubeObj, "v 0 0 0\n" + "v 63 0 0\n" + "v 63 63 0\n" + "v 0 63 0\n" + "v 0 0 63\n" + "v 63 0 63\n" + "v 63 63 63\n" + "v 0 63 63\n" + "f 1 3 2\nf 1 4 3\nf 5 6 7\nf 5 7 8\n" + "f 1 2 6\nf 1 6 5\nf 2 3 7\nf 2 7 6\n" + "f 3 4 8\nf 3 8 7\nf 4 1 5\nf 4 5 8\n", new UTF8Encoding(false));
        }

        private void EnsureAtlasMetadata()
        {
            if (!File.Exists(MarsIndex))
            {
                using StreamWriter writer = new(MarsIndex, false, new UTF8Encoding(false));
                writer.WriteLine("label,hemisphere,lobe,nameFS,name,fullName,BA,color");
                for (int label = 1; label <= 124; ++label)
                {
                    writer.WriteLine($"{label},L,Frontal,fs_{label},Area{label},Area {label},0,255 0 0");
                }
            }

            if (!File.Exists(Brodmann))
            {
                File.WriteAllText(Brodmann, "BA0\n", new UTF8Encoding(false));
            }
        }

        private static void WriteInt16(byte[] buffer, int offset, short value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(short));
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(int));
        }

        private static void WriteSingle(byte[] buffer, int offset, float value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(float));
        }
    }

    internal static class NativePerformanceEnumerableExtensions
    {
        public static int CountLinesStartingWith(this System.Collections.Generic.IEnumerable<string> lines, string prefix)
        {
            int count = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    ++count;
                }
            }

            return count;
        }
    }
}
