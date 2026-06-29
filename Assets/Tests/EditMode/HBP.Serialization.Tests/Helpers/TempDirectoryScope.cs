using System;
using System.IO;

namespace HBP.Tests.Serialization.Helpers
{
    internal sealed class TempDirectoryScope : IDisposable
    {
        public string Path { get; }

        public TempDirectoryScope()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hibop-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string GetPath(params string[] parts)
        {
            string path = Path;
            foreach (string part in parts)
            {
                path = System.IO.Path.Combine(path, part);
            }
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
