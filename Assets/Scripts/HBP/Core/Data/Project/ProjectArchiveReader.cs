using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using Ionic.Zip;

namespace HBP.Core.Data
{
    internal sealed class ProjectArchiveReader : IDisposable
    {
        private readonly List<ZipFile> m_Readers = new();
        private readonly ConcurrentQueue<ZipFile> m_AvailableReaders = new();
        private readonly SemaphoreSlim m_AvailableReaderSlots;

        public ProjectArchiveReader(string path, int readerCount)
        {
            readerCount = Math.Max(1, readerCount);
            m_AvailableReaderSlots = new SemaphoreSlim(readerCount, readerCount);
            try
            {
                for (int i = 0; i < readerCount; i++)
                {
                    ZipFile reader = ZipFile.Read(path);
                    m_Readers.Add(reader);
                    m_AvailableReaders.Enqueue(reader);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public async UniTask<T> ReadAsync<T>(
            ProjectManifest manifest,
            string entryName,
            CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            await m_AvailableReaderSlots.WaitAsync(token);
            ZipFile reader = null;
            try
            {
                if (!m_AvailableReaders.TryDequeue(out reader))
                {
                    throw new InvalidOperationException("No project archive reader is available.");
                }

                ZipEntry entry = reader[entryName];
                if (entry == null)
                {
                    throw new FileNotFoundException(entryName, manifest.Path);
                }

                using Stream stream = entry.OpenReader();
                return ClassLoaderSaver.LoadFromJson<T>(stream);
            }
            finally
            {
                if (reader != null)
                {
                    m_AvailableReaders.Enqueue(reader);
                }
                m_AvailableReaderSlots.Release();
            }
        }

        public void Dispose()
        {
            foreach (ZipFile reader in m_Readers)
            {
                reader.Dispose();
            }
            m_Readers.Clear();
            while (m_AvailableReaders.TryDequeue(out _))
            {
            }
            m_AvailableReaderSlots?.Dispose();
        }
    }
}
