using System;
using System.IO;
using HBP.Core.Data;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public static class P00BaselineGoldenCli
    {
        private const string OutputArgument = "-hbpP00GoldenOutput";

        [MenuItem("Tools/HiBoP/XR/Regenerate P00 Synthetic Golden Buffers")]
        public static void RegenerateFromMenu()
        {
            string outputPath = DefaultOutputPath;
            Write(outputPath);
            AssetDatabase.Refresh();
            Debug.Log($"P00 synthetic golden buffers regenerated: {outputPath}");
        }

        public static void Run()
        {
            bool succeeded = false;
            try
            {
                string outputPath = ReadOptionalArgument(Environment.GetCommandLineArgs(), OutputArgument) ?? DefaultOutputPath;
                Write(outputPath);
                Debug.Log($"P00 synthetic golden buffers regenerated: {outputPath}");
                succeeded = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(succeeded ? 0 : 1);
                }
            }
        }

        internal static JObject Generate()
        {
            JObject d0 = LoadFixture("D0");
            JObject d5 = LoadFixture("D5");
            return new JObject
            {
                ["schemaVersion"] = 1,
                ["algorithm"] = "P00BaselineGolden/v1",
                ["d0Surface"] = new JObject
                {
                    ["vertices"] = d0["vertices"].DeepClone(),
                    ["triangles"] = d0["triangles"].DeepClone(),
                    ["values"] = d0["surfaceValues"].DeepClone()
                },
                ["d0Sites"] = new JObject
                {
                    ["sites"] = d0["sites"].DeepClone(),
                    ["expectedPickedSiteId"] = d0["expectedPickedSiteId"].DeepClone()
                },
                ["d0Cut"] = GenerateCut((JObject)d0["cut"]),
                ["d5Temporal"] = GenerateTemporal(d5)
            };
        }

        internal static string DefaultOutputPath => TestPathUtility.FixturePath("XR", "Baselines", "Expected", "golden-buffers.json");

        private static JObject GenerateCut(JObject cut)
        {
            JArray colors = new();
            foreach (JToken token in (JArray)cut["scalarValues"])
            {
                int value = token.Value<int>();
                colors.Add(new JArray(value * 17, 255 - value * 17, value * 37 % 256, 255));
            }

            return new JObject
            {
                ["width"] = cut["width"].DeepClone(),
                ["height"] = cut["height"].DeepClone(),
                ["rgba32"] = colors
            };
        }

        private static JObject GenerateTemporal(JObject fixture)
        {
            float[] frame0 = fixture["frame0"].ToObject<float[]>();
            float[] frame1 = fixture["frame1"].ToObject<float[]>();
            JArray samples = new();
            foreach (JObject definition in fixture["samples"].Children<JObject>())
            {
                TemporalSample sample = new(definition["index"].Value<int>(), definition["alpha"].Value<float>());
                JArray values = new();
                for (int index = 0; index < frame0.Length; ++index)
                {
                    values.Add(sample.Evaluate(new[] { frame0[index], frame1[index] }));
                }

                samples.Add(new JObject
                {
                    ["index"] = sample.Index,
                    ["alpha"] = sample.Alpha,
                    ["values"] = values
                });
            }

            return new JObject { ["samples"] = samples };
        }

        private static JObject LoadFixture(string id)
        {
            string path = TestPathUtility.FixturePath("XR", "Baselines", id, "fixture.json");
            return JObject.Parse(File.ReadAllText(path));
        }

        private static void Write(string path)
        {
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, Generate().ToString(Formatting.Indented) + Environment.NewLine);
        }

        private static string ReadOptionalArgument(string[] arguments, string name)
        {
            for (int index = 0; index + 1 < arguments.Length; ++index)
            {
                if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
