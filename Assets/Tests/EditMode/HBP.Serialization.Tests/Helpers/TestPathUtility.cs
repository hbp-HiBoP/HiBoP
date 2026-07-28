using System.IO;
using UnityEngine;

namespace HBP.Tests.Serialization.Helpers
{
    internal static class TestPathUtility
    {
        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        public static string FixturePath(params string[] parts)
        {
            string path = Path.Combine(ProjectRoot, "Assets", "Tests", "Fixtures");
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }
    }
}
