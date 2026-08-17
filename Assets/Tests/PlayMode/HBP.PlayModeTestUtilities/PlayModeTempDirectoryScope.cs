using System;
using System.IO;

namespace HBP.Tests.PlayMode.Utilities
{
    public sealed class PlayModeTempDirectoryScope : IDisposable
    {
        public string Path { get; }

        public PlayModeTempDirectoryScope()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hibop-playmode-tests", Guid.NewGuid().ToString("N"));
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
