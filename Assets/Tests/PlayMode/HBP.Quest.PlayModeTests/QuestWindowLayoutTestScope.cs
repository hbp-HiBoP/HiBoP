#if UNITY_EDITOR
using System;
using System.IO;
using HBP.Quest;

namespace HBP.Tests.Quest
{
    /// <summary>Never let window fixtures read or overwrite the headset/user's real layout.</summary>
    internal sealed class QuestWindowLayoutTestScope : IDisposable
    {
        private readonly string previous = QuestWindowLayoutStore.DefaultPath;
        private readonly string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HiBoP-window-layout-" + Guid.NewGuid().ToString("N"));
        public string Path => QuestWindowLayoutStore.DefaultPath;

        public QuestWindowLayoutTestScope()
        {
            Directory.CreateDirectory(folder);
            QuestWindowLayoutStore.DefaultPath = System.IO.Path.Combine(folder, "layout.json");
        }

        public void Dispose()
        {
            QuestWindowLayoutStore.DefaultPath = previous;
            Directory.Delete(folder, true);
        }
    }
}
#endif
