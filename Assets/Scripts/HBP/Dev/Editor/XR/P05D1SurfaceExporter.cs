using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using HBP.Core.DLL;
using HBP.RenderModelAdapters;
using UnityEngine;

namespace HBP.Dev.XR.Editor
{
    public static class P05D1SurfaceExporter
    {
        private const ulong Magic = 0x3530505258425048UL;
        private const ushort SchemaVersion = 1;
        private static readonly AssetHash CaptureHash = AssetHash.Parse("0500000000000000000000000000000000000000000000000000000000000001");

        public static void Export()
        {
            string outputDirectory = GetArgument("-p05D1Output");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Missing -p05D1Output.");
            }

            string meshDirectory = Path.Combine(Application.dataPath, "Data", "Meshes");
            string transformPath = Path.Combine(meshDirectory, "MNI.trm");
            Directory.CreateDirectory(outputDirectory);
            SurfaceAsset anatomical = ExportSurface(outputDirectory, "P05D1Anatomical.bytes", SurfaceRepresentation.Anatomical, meshDirectory, transformPath, "MNI_Lhemi.gii", "MNI_Rhemi.gii");
            SurfaceAsset inflated = ExportSurface(outputDirectory, "P05D1Inflated.bytes", SurfaceRepresentation.Inflated, meshDirectory, transformPath, "MNI_Lwhite_inflated.gii", "MNI_Rwhite_inflated.gii");
            string goldenDirectory = GetArgument("-p05GoldenOutput");
            if (!string.IsNullOrWhiteSpace(goldenDirectory))
            {
                CaptureDesktopGoldens(Path.GetFullPath(goldenDirectory), anatomical, inflated);
            }

            Debug.Log($"P05 D1 GIFTI SurfaceAssets exported to {Path.GetFullPath(outputDirectory)}");
        }

        private static SurfaceAsset ExportSurface(string outputDirectory, string outputName, SurfaceRepresentation representation, string meshDirectory, string transformPath, string leftName, string rightName)
        {
            string leftPath = Path.Combine(meshDirectory, leftName);
            string rightPath = Path.Combine(meshDirectory, rightName);
            using Surface left = Load(leftPath, transformPath);
            using Surface right = Load(rightPath, transformPath);
            using var combined = (Surface)left.Clone();
            combined.Append(right);
            combined.ComputeNormals();

            SurfaceAsset captured = DesktopSurfaceRenderModelAdapter.CaptureAsset(combined, CaptureHash, representation);
            byte[] payload = SerializePayload(captured);
            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(payload);
            }

            string outputPath = Path.Combine(outputDirectory, outputName);
            using FileStream stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write(hash);
            writer.Write(payload);
            Debug.Log($"P05 D1 {representation} | vertices={captured.Positions.Count} faces={captured.Indices.Count / 3} sha256={ToHex(hash)} output={outputPath}");
            return captured;
        }

        private static void CaptureDesktopGoldens(string outputDirectory, SurfaceAsset anatomical, SurfaceAsset inflated)
        {
            Directory.CreateDirectory(outputDirectory);
            Shader shader = Shader.Find("HBP/Brain");
            if (shader == null || !shader.isSupported)
            {
                throw new InvalidOperationException("The Desktop HBP/Brain reference shader is unavailable.");
            }

            CaptureDesktopGolden(outputDirectory, anatomical, "anatomical", shader);
            CaptureDesktopGolden(outputDirectory, inflated, "inflated", shader);
        }

        private static void CaptureDesktopGolden(string outputDirectory, SurfaceAsset asset, string representation, Shader shader)
        {
            Mesh mesh = CreateMesh(asset);
            Material material = new(shader);
            material.SetColor("_Color", new Color(0.72f, 0.72f, 0.74f, 1f));
            material.SetFloat("_AmbientStrength", 0.35f);
            material.SetFloat("_DiffuseStrength", 0.65f);
            material.SetFloat("_Glossiness", 0.45f);
            try
            {
                CaptureView(outputDirectory, mesh, material, representation, "front", Vector3.forward, Vector3.up);
                CaptureView(outputDirectory, mesh, material, representation, "right", Vector3.right, Vector3.up);
                CaptureView(outputDirectory, mesh, material, representation, "top", Vector3.up, Vector3.forward);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static void CaptureView(string outputDirectory, Mesh mesh, Material material, string representation, string viewName, Vector3 direction, Vector3 up)
        {
            const int layer = 31;
            const int size = 512;
            GameObject cameraObject = new("P05 Desktop Golden Camera");
            GameObject surfaceObject = new("P05 Desktop Golden Surface");
            RenderTexture renderTexture = null;
            Texture2D readback = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                surfaceObject.layer = layer;
                surfaceObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                surfaceObject.AddComponent<MeshRenderer>().sharedMaterial = material;

                Bounds bounds = mesh.bounds;
                float radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = radius * 1.08f;
                camera.nearClipPlane = radius;
                camera.farClipPlane = radius * 7f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << layer;
                cameraObject.transform.position = bounds.center + direction * (radius * 4f);
                cameraObject.transform.rotation = Quaternion.LookRotation(-direction, up);

                renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                readback = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                readback.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
                readback.Apply(false, false);

                string stem = $"desktop-{representation}-{viewName}";
                File.WriteAllBytes(Path.Combine(outputDirectory, stem + ".png"), readback.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(outputDirectory, stem + ".rgba32"), readback.GetRawTextureData());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }

                UnityEngine.Object.DestroyImmediate(readback);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(surfaceObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Mesh CreateMesh(SurfaceAsset asset)
        {
            int vertexCount = asset.Positions.Count;
            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var indices = new int[asset.Indices.Count];
            for (int index = 0; index < vertexCount; index++)
            {
                Float3 position = asset.Positions[index];
                Float3 normal = asset.Normals[index];
                positions[index] = new Vector3(position.X, position.Y, position.Z) * asset.CoordinateSpace.MetersPerUnit;
                normals[index] = new Vector3(normal.X, normal.Y, normal.Z);
            }

            for (int index = 0; index < indices.Length; index++)
            {
                indices[index] = checked((int)asset.Indices[index]);
            }

            var mesh = new Mesh
            {
                name = $"P05 Desktop Golden {asset.Representation}",
                indexFormat = vertexCount <= ushort.MaxValue ? UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = positions,
                normals = normals,
            };
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
            return mesh;
        }

        private static Surface Load(string path, string transformPath)
        {
            var surface = new Surface();
            try
            {
                if (!surface.LoadGIIFile(path, transformPath))
                {
                    throw new InvalidOperationException($"Unable to load reference GIFTI '{path}'.");
                }

                surface.FlipTriangles();
                surface.ComputeNormals();
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static byte[] SerializePayload(SurfaceAsset asset)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((byte)asset.Representation);
            ComputeExactBounds(asset.Positions, out Float3 minimum, out Float3 maximum);
            Write(writer, minimum);
            Write(writer, maximum);
            writer.Write(asset.Positions.Count);
            writer.Write(asset.Indices.Count);
            writer.Write(asset.StaticUvs.Count);
            Write(writer, asset.Positions);
            Write(writer, asset.Normals);
            for (int index = 0; index < asset.Indices.Count; index++)
            {
                writer.Write(asset.Indices[index]);
            }

            for (int index = 0; index < asset.StaticUvs.Count; index++)
            {
                writer.Write(asset.StaticUvs[index].X);
                writer.Write(asset.StaticUvs[index].Y);
            }

            writer.Flush();
            return stream.ToArray();
        }

        private static void ComputeExactBounds(RenderBuffer<Float3> positions, out Float3 minimum, out Float3 maximum)
        {
            minimum = positions[0];
            maximum = positions[0];
            for (int index = 1; index < positions.Count; index++)
            {
                Float3 position = positions[index];
                minimum = new Float3(Math.Min(minimum.X, position.X), Math.Min(minimum.Y, position.Y), Math.Min(minimum.Z, position.Z));
                maximum = new Float3(Math.Max(maximum.X, position.X), Math.Max(maximum.Y, position.Y), Math.Max(maximum.Z, position.Z));
            }
        }

        private static void Write(BinaryWriter writer, RenderBuffer<Float3> values)
        {
            for (int index = 0; index < values.Count; index++)
            {
                Write(writer, values[index]);
            }
        }

        private static void Write(BinaryWriter writer, Float3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        private static string ToHex(IReadOnlyList<byte> bytes)
        {
            var characters = new char[bytes.Count * 2];
            const string alphabet = "0123456789abcdef";
            for (int index = 0; index < bytes.Count; index++)
            {
                characters[index * 2] = alphabet[bytes[index] >> 4];
                characters[index * 2 + 1] = alphabet[bytes[index] & 0x0f];
            }

            return new string(characters);
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
