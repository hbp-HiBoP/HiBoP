using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Quest
{
    /// <summary>Reads the triangle geometry of the Quest render-model GLB. The contour uses HiBoP's own material.</summary>
    internal sealed class QuestControllerModelGeometry
    {
        internal const int MaxVisibilitySamples = 24;
        private const int ControlCount = (int)QuestControllerControl.SideGrip + 1;
        private readonly Vector3[] anchors;
        private readonly Vector3[][] visibilitySamples;
        private readonly int anchoredControls;
        public Mesh Mesh { get; }

        private QuestControllerModelGeometry(Mesh mesh, Vector3[] anchors, Vector3[][] visibilitySamples, int anchoredControls)
        {
            Mesh = mesh;
            this.anchors = anchors;
            this.visibilitySamples = visibilitySamples;
            this.anchoredControls = anchoredControls;
        }

        public Vector3 Anchor(QuestControllerControl control) => anchors[(int)control];
        public Vector3[] VisibilitySamples(QuestControllerControl control) => visibilitySamples[(int)control];
        public bool HasAnchor(QuestControllerControl control) => (anchoredControls & (1 << (int)control)) != 0;

        public static QuestControllerModelGeometry Read(byte[] bytes, bool left)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != 0x46546c67 || reader.ReadUInt32() != 2 || reader.ReadUInt32() != bytes.Length)
                throw new FormatException("Expected a glTF 2.0 binary render model.");

            int jsonLength = reader.ReadInt32();
            if (reader.ReadUInt32() != 0x4e4f534a || jsonLength <= 0 || jsonLength > stream.Length - stream.Position - 8)
                throw new FormatException("The render model has an invalid GLB JSON chunk.");
            var document = JObject.Parse(Encoding.UTF8.GetString(reader.ReadBytes(jsonLength)));
            int binLength = reader.ReadInt32();
            if (reader.ReadUInt32() != 0x004e4942 || binLength <= 0 || binLength > stream.Length - stream.Position)
                throw new FormatException("The render model has an invalid GLB geometry chunk.");

            return new GeometryReader(reader, document, stream.Position, binLength, left).Read();
        }

        private sealed class GeometryReader
        {
            private readonly BinaryReader reader;
            private readonly JObject document;
            private readonly long binaryStart;
            private readonly int binaryLength;
            private readonly bool left;
            private readonly List<Vector3> vertices = new();
            private readonly List<Vector3> normals = new();
            private readonly List<int> triangles = new();
            private readonly List<int>[] controlTriangles = new List<int>[ControlCount];
            private readonly List<(int Node, int Mesh, QuestControllerControl? Control)> meshNodes = new();
            private Matrix4x4[] nodeTransforms;

            public GeometryReader(BinaryReader reader, JObject document, long binaryStart, int binaryLength, bool left)
            {
                this.reader = reader;
                this.document = document;
                this.binaryStart = binaryStart;
                this.binaryLength = binaryLength;
                this.left = left;
            }

            public QuestControllerModelGeometry Read()
            {
                var nodes = document["nodes"] as JArray;
                var root = document["scenes"]?[document.Value<int?>("scene") ?? 0]?["nodes"] as JArray;
                if (nodes == null || root == null) throw new FormatException("The render model has no scene.");
                // The runtime model is already in OpenXR grip space. Unity flips Z when importing that pose.
                var conversion = Matrix4x4.Scale(new Vector3(1, 1, -1));
                nodeTransforms = new Matrix4x4[nodes.Count];
                foreach (var index in root) Visit(index.Value<int>(), conversion, null, 0);
                foreach (var item in meshNodes)
                {
                    var node = nodes[item.Node];
                    var sourceMesh = document["meshes"][item.Mesh];
                    string name = (node.Value<string>("name") ?? "") + (sourceMesh.Value<string>("name") ?? "");
                    if (name.IndexOf("batteryIndicator", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    var skin = node.Value<int?>("skin") is { } skinIndex ? ReadSkin(skinIndex) : null;
                    foreach (var primitive in (JArray)sourceMesh["primitives"])
                        AddPrimitive(primitive, nodeTransforms[item.Node], item.Control ?? MatchControl(sourceMesh.Value<string>("name") ?? "", left), skin);
                }

                if (triangles.Count == 0) throw new FormatException("The render model has no triangles.");

                var anchors = new Vector3[ControlCount];
                var visibilitySamples = new Vector3[ControlCount][];
                int available = 0;
                var missing = new List<QuestControllerControl>();
                for (int i = 0; i < anchors.Length; i++)
                {
                    var control = (QuestControllerControl)i;
                    if (controlTriangles[i] is not { } part)
                    {
                        if (!left || control != QuestControllerControl.SecondaryButton) missing.Add(control);
                        continue;
                    }

                    anchors[i] = SurfaceAnchor(part, control);
                    visibilitySamples[i] = SampleSurface(part, anchors[i]);
                    available |= 1 << i;
                }

                if (missing.Count != 0)
                    Debug.LogWarning($"[Quest controller models] Native {(left ? "left" : "right")} model has no surface for: {string.Join(", ", missing)}.");

                var mesh = new Mesh { name = left ? "OpenXR Left Controller" : "OpenXR Right Controller" };
                if (vertices.Count > ushort.MaxValue) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                if (normals.Count == vertices.Count) mesh.SetNormals(normals);
                else mesh.RecalculateNormals();
                var controlMasks = new int[vertices.Count];
                for (int i = 0; i < ControlCount; i++)
                    if (controlTriangles[i] is { } part)
                        foreach (int vertex in part)
                            controlMasks[vertex] |= 1 << i;
                var controlUv = new Vector2[vertices.Count];
                for (int i = 0; i < controlUv.Length; i++) controlUv[i] = new Vector2(controlMasks[i], 0);
                mesh.SetUVs(1, controlUv);
                return new QuestControllerModelGeometry(mesh, anchors, visibilitySamples, available);
            }

            private void Visit(int index, Matrix4x4 parent, QuestControllerControl? parentControl, int depth)
            {
                if (depth > 128) throw new FormatException("The render model hierarchy is too deep.");
                var node = document["nodes"][index];
                var matrix = parent * LocalTransform(node);
                nodeTransforms[index] = matrix;
                var control = MatchControl(node.Value<string>("name") ?? "", left) ?? parentControl;
                if (node.Value<int?>("mesh") is { } meshIndex)
                    meshNodes.Add((index, meshIndex, control));

                if (node["children"] is JArray children)
                    foreach (var child in children)
                        Visit(child.Value<int>(), matrix, control, depth + 1);
            }

            private void AddPrimitive(JToken primitive, Matrix4x4 matrix, QuestControllerControl? control, Skin skin)
            {
                if ((primitive.Value<int?>("mode") ?? 4) != 4)
                    throw new FormatException("The render model contains a non-triangle primitive.");
                var attributes = primitive["attributes"];
                if (attributes?.Value<int?>("POSITION") is not { } positionIndex)
                    throw new FormatException("The render model has no uncompressed positions.");
                var positions = GetAccessor(positionIndex);
                var meshNormals = attributes.Value<int?>("NORMAL") is { } normalIndex ? GetAccessor(normalIndex) : (Accessor?)null;
                int start = vertices.Count;
                if (positions.Count > 250000 - start || (meshNormals.HasValue && meshNormals.Value.Count != positions.Count))
                    throw new FormatException("The render model has invalid vertex counts.");

                QuestControllerControl?[] vertexControls = null;
                if (skin == null)
                {
                    ReadVectors(positions, matrix, vertices, normal: false);
                    if (meshNormals.HasValue) ReadVectors(meshNormals.Value, matrix.inverse.transpose, normals, normal: true);
                }
                else
                    vertexControls = ReadSkinnedVertices(attributes, skin, positions, meshNormals);

                var indices = primitive.Value<int?>("indices") is { } index ? GetAccessor(index) : (Accessor?)null;
                if (indices.HasValue && indices.Value.Type != "SCALAR")
                    throw new FormatException("The render model has invalid index data.");
                int count = indices?.Count ?? positions.Count;
                if (count % 3 != 0 || count > 750000 - triangles.Count)
                    throw new FormatException("The render model has an invalid triangle count.");

                bool flip = (skin == null ? matrix : skin.Transforms[0]).determinant < 0;
                for (int i = 0; i < count; i += 3)
                {
                    int a = start + ReadIndex(i);
                    int b = start + ReadIndex(i + (flip ? 2 : 1));
                    int c = start + ReadIndex(i + (flip ? 1 : 2));
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    QuestControllerControl? partControl = control;
                    if (!partControl.HasValue && vertexControls != null)
                    {
                        var first = vertexControls[a - start];
                        var second = vertexControls[b - start];
                        var third = vertexControls[c - start];
                        partControl = first.HasValue && (first == second || first == third) ? first : second == third ? second : null;
                    }

                    if (!partControl.HasValue) continue;
                    var part = controlTriangles[(int)partControl.Value] ??= new List<int>();
                    part.Add(a);
                    part.Add(b);
                    part.Add(c);
                }

                int ReadIndex(int item)
                {
                    if (!indices.HasValue) return item;
                    var data = indices.Value;
                    reader.BaseStream.Position = data.Start + (long)item * data.Stride;
                    uint vertex = data.Component switch
                    {
                        5121 => reader.ReadByte(),
                        5123 => reader.ReadUInt16(),
                        5125 => reader.ReadUInt32(),
                        _ => throw new FormatException("The render model uses unsupported indices.")
                    };
                    if (vertex >= positions.Count) throw new FormatException("A render-model index is outside its vertices.");
                    return (int)vertex;
                }
            }

            private sealed class Skin
            {
                public readonly Matrix4x4[] Transforms, NormalTransforms;
                public readonly QuestControllerControl?[] Controls;

                public Skin(int count)
                {
                    Transforms = new Matrix4x4[count];
                    NormalTransforms = new Matrix4x4[count];
                    Controls = new QuestControllerControl?[count];
                }
            }

            private Skin ReadSkin(int index)
            {
                var skin = document["skins"][index];
                if (skin["joints"] is not JArray joints || joints.Count == 0)
                    throw new FormatException("The render model has a skin without joints.");
                var bind = skin.Value<int?>("inverseBindMatrices") is { } bindIndex ? GetAccessor(bindIndex) : (Accessor?)null;
                if (bind.HasValue && (bind.Value.Type != "MAT4" || bind.Value.Component != 5126 || bind.Value.Count < joints.Count))
                    throw new FormatException("The render model has invalid inverse bind matrices.");
                var result = new Skin(joints.Count);
                var names = new string[joints.Count];
                for (int i = 0; i < joints.Count; i++)
                {
                    int joint = joints[i].Value<int>();
                    var inverseBind = Matrix4x4.identity;
                    if (bind.HasValue)
                    {
                        reader.BaseStream.Position = bind.Value.Start + (long)i * bind.Value.Stride;
                        for (int element = 0; element < 16; element++) inverseBind[element] = reader.ReadSingle();
                    }

                    // A skinned vertex is already in the common frame after applying its joint matrices.
                    result.Transforms[i] = nodeTransforms[joint] * inverseBind;
                    result.NormalTransforms[i] = result.Transforms[i].inverse.transpose;
                    names[i] = document["nodes"][joint].Value<string>("name") ?? "";
                    result.Controls[i] = MatchControl(names[i], left);
                }

                Debug.Log($"[Quest controller models] {(left ? "Left" : "Right")} model joints: {string.Join(", ", names)}.");
                return result;
            }

            private QuestControllerControl?[] ReadSkinnedVertices(JToken attributes, Skin skin, Accessor positions, Accessor? meshNormals)
            {
                if (attributes.Value<int?>("JOINTS_0") is not { } jointIndex || attributes.Value<int?>("WEIGHTS_0") is not { } weightIndex || attributes["JOINTS_1"] != null)
                    throw new FormatException("The render model uses unsupported joint attributes.");
                var joints = GetAccessor(jointIndex);
                var weights = GetAccessor(weightIndex);
                if (joints.Type != "VEC4" || weights.Type != "VEC4" || joints.Count != positions.Count || weights.Count != positions.Count)
                    throw new FormatException("The render model has mismatched joint attributes.");

                var controls = new QuestControllerControl?[positions.Count];
                Span<int> jointIds = stackalloc int[4];
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector3 position = ReadVector(positions, i);
                    Vector3 normal = meshNormals.HasValue ? ReadVector(meshNormals.Value, i) : Vector3.zero;
                    reader.BaseStream.Position = joints.Start + (long)i * joints.Stride;
                    for (int influence = 0; influence < 4; influence++)
                        jointIds[influence] = joints.Component switch
                        {
                            5121 => reader.ReadByte(),
                            5123 => reader.ReadUInt16(),
                            _ => throw new FormatException("The render model uses unsupported joint indices.")
                        };
                    reader.BaseStream.Position = weights.Start + (long)i * weights.Stride;
                    Vector3 bakedPosition = Vector3.zero, bakedNormal = Vector3.zero;
                    float weightSum = 0, strongest = 0;
                    for (int influence = 0; influence < 4; influence++)
                    {
                        int joint = jointIds[influence];
                        float weight = weights.Component switch
                        {
                            5121 => reader.ReadByte() / 255f,
                            5123 => reader.ReadUInt16() / 65535f,
                            5126 => reader.ReadSingle(),
                            _ => throw new FormatException("The render model uses unsupported joint weights.")
                        };
                        if (weight == 0) continue;
                        if (joint >= skin.Transforms.Length || weight < 0 || !float.IsFinite(weight))
                            throw new FormatException("The render model contains an invalid joint influence.");
                        bakedPosition += weight * skin.Transforms[joint].MultiplyPoint3x4(position);
                        if (meshNormals.HasValue) bakedNormal += weight * skin.NormalTransforms[joint].MultiplyVector(normal);
                        weightSum += weight;
                        if (weight <= strongest) continue;
                        strongest = weight;
                        controls[i] = skin.Controls[joint];
                    }

                    if (weightSum <= 0) throw new FormatException("A render-model vertex has no joint weight.");
                    vertices.Add(bakedPosition / weightSum);
                    if (meshNormals.HasValue) normals.Add(bakedNormal.normalized);
                }

                return controls;
            }

            private void ReadVectors(Accessor data, Matrix4x4 matrix, List<Vector3> target, bool normal)
            {
                if (data.Type != "VEC3" || data.Component != 5126)
                    throw new FormatException("The render model uses unsupported vertex data.");
                for (int i = 0; i < data.Count; i++)
                {
                    var point = ReadVector(data, i);
                    var value = normal ? matrix.MultiplyVector(point).normalized : matrix.MultiplyPoint3x4(point);
                    if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                        throw new FormatException("The render model contains a non-finite vertex.");
                    target.Add(value);
                }
            }

            private Vector3 ReadVector(Accessor data, int index)
            {
                if (data.Type != "VEC3" || data.Component != 5126)
                    throw new FormatException("The render model uses unsupported vertex data.");
                reader.BaseStream.Position = data.Start + (long)index * data.Stride;
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            private Accessor GetAccessor(int index)
            {
                var accessor = document["accessors"][index];
                if (accessor["sparse"] != null || accessor.Value<int?>("bufferView") is not { } viewIndex)
                    throw new FormatException("The render model has unsupported sparse or compressed geometry.");
                var view = document["bufferViews"][viewIndex];
                if (view.Value<int>("buffer") != 0) throw new FormatException("The render model geometry must be embedded.");
                string type = accessor.Value<string>("type");
                int components = type switch { "VEC3" => 3, "VEC4" => 4, "MAT4" => 16, "SCALAR" => 1, _ => throw new FormatException("Unsupported accessor shape.") };
                int component = accessor.Value<int>("componentType");
                int size = component switch { 5121 => 1, 5123 => 2, 5125 or 5126 => 4, _ => throw new FormatException("Unsupported accessor component.") };
                int width = components * size;
                int stride = view.Value<int?>("byteStride") ?? width;
                int count = accessor.Value<int>("count");
                int offset = accessor.Value<int>("byteOffset");
                int viewOffset = view.Value<int>("byteOffset");
                int viewLength = view.Value<int>("byteLength");
                if (count <= 0 || offset < 0 || viewOffset < 0 || viewLength < 0 || stride < width || (long)offset + (long)(count - 1) * stride + width > viewLength || (long)viewOffset + viewLength > binaryLength)
                    throw new FormatException("Render-model geometry lies outside its GLB buffer.");
                return new Accessor(binaryStart + viewOffset + offset, count, stride, component, type);
            }

            private readonly struct Accessor
            {
                public readonly long Start;
                public readonly int Count, Stride, Component;
                public readonly string Type;

                public Accessor(long start, int count, int stride, int component, string type)
                {
                    Start = start;
                    Count = count;
                    Stride = stride;
                    Component = component;
                    Type = type;
                }
            }

            private Vector3 SurfaceAnchor(List<int> part, QuestControllerControl control)
            {
                var bounds = new Bounds(vertices[part[0]], Vector3.zero);
                foreach (int index in part) bounds.Encapsulate(vertices[index]);
                Vector3 outside = control switch
                {
                    QuestControllerControl.FrontTrigger => Vector3.down,
                    QuestControllerControl.SideGrip => left ? Vector3.right : Vector3.left,
                    _ => new Vector3(0, .8f, .6f)
                };
                var ray = new Ray(bounds.center + outside * (bounds.size.magnitude + .01f), -outside);
                float hit = float.PositiveInfinity;
                for (int i = 0; i < part.Count; i += 3)
                    if (Intersect(ray, vertices[part[i]], vertices[part[i + 1]], vertices[part[i + 2]], out float distance))
                        hit = Mathf.Min(hit, distance);
                if (float.IsFinite(hit)) return ray.GetPoint(hit);

                // A concave part can have an empty center. Choose its nearest vertex to the ray origin.
                Vector3 nearest = vertices[part[0]];
                foreach (int index in part)
                    if ((vertices[index] - ray.origin).sqrMagnitude < (nearest - ray.origin).sqrMagnitude)
                        nearest = vertices[index];
                return nearest;
            }

            private Vector3[] SampleSurface(List<int> part, Vector3 anchor)
            {
                int count = part.Count / 3;
                var centers = new Vector3[count];
                var distances = new float[count];
                for (int i = 0; i < count; i++)
                {
                    int index = i * 3;
                    centers[i] = (vertices[part[index]] + vertices[part[index + 1]] + vertices[part[index + 2]]) / 3;
                    distances[i] = float.PositiveInfinity;
                }

                // Spread the points across the control while keeping them inside its triangles.
                var samples = new Vector3[Mathf.Min(count, MaxVisibilitySamples)];
                Vector3 previous = anchor;
                for (int i = 0; i < samples.Length; i++)
                {
                    int farthest = 0;
                    for (int j = 0; j < count; j++)
                    {
                        distances[j] = Mathf.Min(distances[j], (centers[j] - previous).sqrMagnitude);
                        if (distances[j] > distances[farthest]) farthest = j;
                    }

                    samples[i] = previous = centers[farthest];
                    distances[farthest] = -1;
                }

                return samples;
            }
        }

        private static bool Intersect(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
        {
            distance = 0;
            Vector3 ab = b - a, ac = c - a;
            Vector3 p = Vector3.Cross(ray.direction, ac);
            float determinant = Vector3.Dot(ab, p);
            if (Mathf.Abs(determinant) < 1e-10f) return false;
            Vector3 offset = ray.origin - a;
            float u = Vector3.Dot(offset, p) / determinant;
            Vector3 q = Vector3.Cross(offset, ab);
            float v = Vector3.Dot(ray.direction, q) / determinant;
            distance = Vector3.Dot(ac, q) / determinant;
            return u >= 0 && v >= 0 && u + v <= 1 && distance >= 0;
        }

        private static Matrix4x4 LocalTransform(JToken node)
        {
            if (node["matrix"] is JArray values)
            {
                var matrix = new Matrix4x4();
                for (int i = 0; i < 16; i++) matrix[i % 4, i / 4] = values[i].Value<float>();
                return matrix;
            }

            var translation = Vector(node["translation"], Vector3.zero);
            var scale = Vector(node["scale"], Vector3.one);
            var rotation = node["rotation"] is JArray r ? new Quaternion(r[0].Value<float>(), r[1].Value<float>(), r[2].Value<float>(), r[3].Value<float>()) : Quaternion.identity;
            return Matrix4x4.TRS(translation, rotation, scale);

            static Vector3 Vector(JToken value, Vector3 fallback) => value is JArray a ? new Vector3(a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>()) : fallback;
        }

        private static QuestControllerControl? MatchControl(string name, bool left)
        {
            // Meta button joints use b_ as a bone prefix.
            string key = name.ToLowerInvariant().Replace("b_button_", "button_").Replace("_", "").Replace("-", "").Replace(" ", "").Replace(".", "");
            if (key.Contains("thumbstick") || key.Contains("joystick")) return QuestControllerControl.Thumbstick;
            if (key.Contains("squeeze") || key.Contains("griptrigger") || key.Contains("triggergrip") || key.Contains("handtrigger"))
                return QuestControllerControl.SideGrip;
            if (key.Contains("trigger")) return QuestControllerControl.FrontTrigger;
            char primary = left ? 'x' : 'a';
            char secondary = left ? 'y' : 'b';
            if (IsButton(primary)) return QuestControllerControl.PrimaryButton;
            if (IsButton(secondary)) return QuestControllerControl.SecondaryButton;
            return null;

            bool IsButton(char letter) => key == letter.ToString() || key.Contains($"button{letter}") || key.Contains($"{letter}button");
        }
    }
}
