using HBP.Core.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace HBP.Core.Tools
{
    /// <summary>
    /// Temporary loading instrumentation used while optimizing database and project loading.
    /// Remove this file and every TEMP-LOADING-PROFILING call site once the optimization work is complete.
    /// </summary>
    public static class LoadingDiagnostics
    {
        public enum Operation
        {
            Database,
            Project
        }

        public enum Phase
        {
            None,
            DatabaseSettings,
            DatabaseProtocols,
            DatabaseReferences,
            DatabasePatientsRead,
            DatabasePatientsDeserialize,
            DatabasePatientsBindTags,
            DatabasePatientsValidateFiles,
            DatabaseDataInfosRead,
            DatabaseDataInfosDeserialize,
            DatabaseLinkReferences,
            ProjectManifest,
            ProjectArchiveRead,
            ProjectSettings,
            ProjectPatientsRead,
            ProjectPatientsDeserialize,
            ProjectPatientsBindTags,
            ProjectGroups,
            ProjectDatasets,
            ProjectVisualizations,
            ProjectLinkReferences,
            ProjectValidateFiles
        }

        private static readonly object s_Lock = new();
        private static SessionData s_ActiveSession;
        private static string s_OutputDirectoryForTests;
        private static string s_LastSummaryPath;
        private static int s_FileSequence;
        private static string s_Platform = "Unknown";
        private static string s_UnityVersion = "Unknown";
        private static string s_DefaultOutputDirectory =
            Path.Combine(Directory.GetCurrentDirectory(), "LoadingBenchmarks");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureUnityRuntimeMetadata()
        {
            lock (s_Lock)
            {
                s_Platform = Application.platform.ToString();
                s_UnityVersion = Application.unityVersion;
                s_DefaultOutputDirectory = Path.Combine(Application.persistentDataPath, "LoadingBenchmarks");
            }
        }

        public static string LastSummaryPath
        {
            get
            {
                lock (s_Lock)
                {
                    return s_LastSummaryPath;
                }
            }
        }

        public static void SetOutputDirectoryForTests(string outputDirectory)
        {
            lock (s_Lock)
            {
                if (s_ActiveSession != null)
                {
                    throw new InvalidOperationException("Loading diagnostic output cannot be changed during an active session.");
                }

                s_OutputDirectoryForTests = outputDirectory;
                s_LastSummaryPath = null;
            }
        }

        public static SessionScope BeginSession(Operation operation)
        {
            lock (s_Lock)
            {
                if (s_ActiveSession != null)
                {
                    return new SessionScope(s_ActiveSession, false);
                }

                s_ActiveSession = new SessionData(operation);
                return new SessionScope(s_ActiveSession, true);
            }
        }

        public static PhaseScope BeginPhase(Phase phase, int fileCount = 0, long byteCount = 0, int objectCount = 0, int concurrency = 0)
        {
            if (phase == Phase.None)
            {
                return default;
            }

            SessionData session;
            lock (s_Lock)
            {
                session = s_ActiveSession;
            }

            return session == null
                ? default
                : new PhaseScope(session, phase, fileCount, byteCount, objectCount, concurrency);
        }

        public static bool FileExists(string path)
        {
            SessionData session;
            lock (s_Lock)
            {
                session = s_ActiveSession;
            }

            if (session == null)
            {
                return File.Exists(path);
            }

            long start = Stopwatch.GetTimestamp();
            try
            {
                return File.Exists(path);
            }
            finally
            {
                session.RecordFileExists(ElapsedMilliseconds(start), ValidationPhase(session.Operation));
            }
        }

        public static void RecordTagLookups(int count)
        {
            if (count <= 0)
            {
                return;
            }

            GetActiveSession()?.RecordTagLookups(count);
        }

        public static void RecordReferenceLookups(int count)
        {
            if (count <= 0)
            {
                return;
            }

            GetActiveSession()?.RecordReferenceLookups(count);
        }

        public static PhaseScope BeginReferenceLink(int lookupCount)
        {
            if (lookupCount <= 0)
            {
                return default;
            }

            SessionData session = GetActiveSession();
            if (session == null)
            {
                return default;
            }

            session.RecordReferenceLookups(lookupCount);
            return new PhaseScope(session, LinkReferencesPhase(session.Operation), 0, 0, 0, 0);
        }

        public static void RecordObjects(string family, int count)
        {
            if (string.IsNullOrEmpty(family) || count <= 0)
            {
                return;
            }

            GetActiveSession()?.RecordObjects(family, count);
        }

        public static void RecordPatientGraph(Patient patient)
        {
            if (patient == null)
            {
                return;
            }

            int siteCount = patient.Sites?.Count ?? 0;
            int coordinateCount = 0;
            int tagValueCount = patient.Tags?.Count ?? 0;
            if (patient.Sites != null)
            {
                foreach (Site site in patient.Sites)
                {
                    coordinateCount += site.Coordinates?.Count ?? 0;
                    tagValueCount += site.Tags?.Count ?? 0;
                }
            }

            RecordObjects("Patient", 1);
            RecordObjects("Mesh", patient.Meshes?.Count ?? 0);
            RecordObjects("MRI", patient.MRIs?.Count ?? 0);
            RecordObjects("Site", siteCount);
            RecordObjects("Coordinate", coordinateCount);
            RecordObjects("TagValue", tagValueCount);
        }

        public static long GetFileLength(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        private static SessionData GetActiveSession()
        {
            lock (s_Lock)
            {
                return s_ActiveSession;
            }
        }

        private static void EndSession(SessionData session)
        {
            lock (s_Lock)
            {
                if (s_ActiveSession == session)
                {
                    s_ActiveSession = null;
                }
            }

            try
            {
                string outputDirectory = string.IsNullOrWhiteSpace(s_OutputDirectoryForTests)
                    ? session.DefaultOutputDirectory
                    : s_OutputDirectoryForTests;
                string runtimeDirectory = Path.Combine(outputDirectory, session.RuntimeKey);
                Directory.CreateDirectory(runtimeDirectory);

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
                int sequence = Interlocked.Increment(ref s_FileSequence);
                string path = Path.Combine(runtimeDirectory, $"loading-{session.Operation.ToString().ToLowerInvariant()}-{timestamp}-{sequence}.json");
                File.WriteAllText(path, JsonUtility.ToJson(session.CreateSummary(), true), new UTF8Encoding(false));

                lock (s_Lock)
                {
                    s_LastSummaryPath = path;
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"Loading diagnostic summary could not be written ({exception.GetType().Name}).");
            }
        }

        private static string PhaseName(Phase phase)
        {
            return phase switch
            {
                Phase.DatabaseSettings => "Loading.Database.Settings",
                Phase.DatabaseProtocols => "Loading.Database.Protocols",
                Phase.DatabaseReferences => "Loading.Database.References",
                Phase.DatabasePatientsRead => "Loading.Database.Patients.Read",
                Phase.DatabasePatientsDeserialize => "Loading.Database.Patients.Deserialize",
                Phase.DatabasePatientsBindTags => "Loading.Database.Patients.BindTags",
                Phase.DatabasePatientsValidateFiles => "Loading.Database.Patients.ValidateFiles",
                Phase.DatabaseDataInfosRead => "Loading.Database.DataInfos.Read",
                Phase.DatabaseDataInfosDeserialize => "Loading.Database.DataInfos.Deserialize",
                Phase.DatabaseLinkReferences => "Loading.Database.LinkReferences",
                Phase.ProjectManifest => "Loading.Project.Manifest",
                Phase.ProjectArchiveRead => "Loading.Project.ArchiveRead",
                Phase.ProjectSettings => "Loading.Project.Settings",
                Phase.ProjectPatientsRead => "Loading.Project.Patients.Read",
                Phase.ProjectPatientsDeserialize => "Loading.Project.Patients.Deserialize",
                Phase.ProjectPatientsBindTags => "Loading.Project.Patients.BindTags",
                Phase.ProjectGroups => "Loading.Project.Groups",
                Phase.ProjectDatasets => "Loading.Project.Datasets",
                Phase.ProjectVisualizations => "Loading.Project.Visualizations",
                Phase.ProjectLinkReferences => "Loading.Project.LinkReferences",
                Phase.ProjectValidateFiles => "Loading.Project.ValidateFiles",
                _ => "Loading.Unknown"
            };
        }

        private static Phase ValidationPhase(Operation operation)
        {
            return operation == Operation.Database
                ? Phase.DatabasePatientsValidateFiles
                : Phase.ProjectValidateFiles;
        }

        private static Phase LinkReferencesPhase(Operation operation)
        {
            return operation == Operation.Database
                ? Phase.DatabaseLinkReferences
                : Phase.ProjectLinkReferences;
        }

        private static double ElapsedMilliseconds(long start)
        {
            return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }

        private static long ProcessCpuTicks()
        {
            try
            {
                using Process process = Process.GetCurrentProcess();
                return process.TotalProcessorTime.Ticks;
            }
            catch
            {
                return -1;
            }
        }

        private static long AllocatedBytesForCurrentThread()
        {
            try
            {
                return GC.GetAllocatedBytesForCurrentThread();
            }
            catch
            {
                return -1;
            }
        }

        public sealed class SessionScope : IDisposable
        {
            private readonly SessionData m_Session;
            private readonly bool m_Owner;
            private bool m_Disposed;

            internal SessionScope(SessionData session, bool owner)
            {
                m_Session = session;
                m_Owner = owner;
            }

            public void MarkSucceeded()
            {
                if (m_Owner)
                {
                    m_Session.MarkSucceeded();
                }
            }

            public void MarkCanceled()
            {
                if (m_Owner)
                {
                    m_Session.MarkCanceled();
                }
            }

            public void MarkFailed(Exception exception)
            {
                if (m_Owner)
                {
                    m_Session.MarkFailed(exception);
                }
            }

            public void Dispose()
            {
                if (!m_Owner || m_Disposed)
                {
                    return;
                }

                m_Disposed = true;
                m_Session.Finish();
                EndSession(m_Session);
            }
        }

        public readonly struct PhaseScope : IDisposable
        {
            private readonly SessionData m_Session;
            private readonly Phase m_Phase;
            private readonly int m_FileCount;
            private readonly long m_ByteCount;
            private readonly int m_ObjectCount;
            private readonly int m_Concurrency;
            private readonly long m_StartTimestamp;
            private readonly long m_StartCpuTicks;
            private readonly long m_StartManagedMemory;
            private readonly long m_StartAllocatedBytes;
            private readonly int m_StartThreadId;
            private readonly int m_StartGc0;
            private readonly int m_StartGc1;
            private readonly int m_StartGc2;

            internal PhaseScope(SessionData session, Phase phase, int fileCount, long byteCount, int objectCount, int concurrency)
            {
                m_Session = session;
                m_Phase = phase;
                m_FileCount = fileCount;
                m_ByteCount = byteCount;
                m_ObjectCount = objectCount;
                m_Concurrency = concurrency;
                m_StartTimestamp = Stopwatch.GetTimestamp();
                m_StartCpuTicks = ProcessCpuTicks();
                m_StartManagedMemory = GC.GetTotalMemory(false);
                m_StartAllocatedBytes = AllocatedBytesForCurrentThread();
                m_StartThreadId = Thread.CurrentThread.ManagedThreadId;
                m_StartGc0 = GC.CollectionCount(0);
                m_StartGc1 = GC.CollectionCount(1);
                m_StartGc2 = GC.CollectionCount(2);
            }

            public void Dispose()
            {
                if (m_Session == null)
                {
                    return;
                }

                long endCpuTicks = ProcessCpuTicks();
                long endManagedMemory = GC.GetTotalMemory(false);
                long endAllocatedBytes = Thread.CurrentThread.ManagedThreadId == m_StartThreadId
                    ? AllocatedBytesForCurrentThread()
                    : -1;

                m_Session.RecordPhase(
                    m_Phase,
                    ElapsedMilliseconds(m_StartTimestamp),
                    m_StartCpuTicks >= 0 && endCpuTicks >= m_StartCpuTicks
                        ? TimeSpan.FromTicks(endCpuTicks - m_StartCpuTicks).TotalMilliseconds
                        : -1,
                    m_FileCount,
                    m_ByteCount,
                    m_ObjectCount,
                    m_Concurrency,
                    m_StartManagedMemory,
                    endManagedMemory,
                    m_StartAllocatedBytes >= 0 && endAllocatedBytes >= m_StartAllocatedBytes
                        ? endAllocatedBytes - m_StartAllocatedBytes
                        : -1,
                    GC.CollectionCount(0) - m_StartGc0,
                    GC.CollectionCount(1) - m_StartGc1,
                    GC.CollectionCount(2) - m_StartGc2);
            }
        }

        internal sealed class SessionData
        {
            private readonly object m_Lock = new();
            private readonly Dictionary<Phase, PhaseAggregate> m_Phases = new();
            private readonly Dictionary<string, long> m_ObjectCounts = new(StringComparer.Ordinal);
            private readonly long m_StartTimestamp = Stopwatch.GetTimestamp();
            private readonly long m_StartCpuTicks = ProcessCpuTicks();
            private readonly long m_StartManagedMemory = GC.GetTotalMemory(false);
            private readonly int m_StartGc0 = GC.CollectionCount(0);
            private readonly int m_StartGc1 = GC.CollectionCount(1);
            private readonly int m_StartGc2 = GC.CollectionCount(2);
            private string m_Status = "Running";
            private string m_FailureType = string.Empty;
            private string m_EndedUtc;
            private double m_TotalWallMilliseconds;
            private double m_TotalCpuMilliseconds = -1;
            private long m_EndManagedMemory;
            private int m_Gc0;
            private int m_Gc1;
            private int m_Gc2;

            public Operation Operation { get; }
            public string StartedUtc { get; } = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            public string Runtime { get; }
            public string ScriptingBackend { get; }
            public string Platform { get; }
            public string UnityVersion { get; }
            public string DefaultOutputDirectory { get; }
            public string RuntimeKey => $"{Runtime}-{ScriptingBackend}-{Platform}";

            public SessionData(Operation operation)
            {
                Operation = operation;
#if UNITY_EDITOR
                Runtime = "Editor";
#else
                Runtime = "Player";
#endif
#if ENABLE_IL2CPP
                ScriptingBackend = "IL2CPP";
#else
                ScriptingBackend = "Mono";
#endif
                Platform = s_Platform;
                UnityVersion = s_UnityVersion;
                DefaultOutputDirectory = s_DefaultOutputDirectory;
            }

            public void RecordPhase(
                Phase phase,
                double wallMilliseconds,
                double cpuMilliseconds,
                int fileCount,
                long byteCount,
                int objectCount,
                int concurrency,
                long managedMemoryBefore,
                long managedMemoryAfter,
                long allocatedBytes,
                int gc0,
                int gc1,
                int gc2)
            {
                lock (m_Lock)
                {
                    GetPhase(phase).Add(
                        wallMilliseconds,
                        cpuMilliseconds,
                        fileCount,
                        byteCount,
                        objectCount,
                        concurrency,
                        managedMemoryBefore,
                        managedMemoryAfter,
                        allocatedBytes,
                        gc0,
                        gc1,
                        gc2);
                }
            }

            public void RecordFileExists(double wallMilliseconds, Phase phase)
            {
                lock (m_Lock)
                {
                    GetPhase(phase).AddFileExists(wallMilliseconds);
                }
            }

            public void RecordTagLookups(int count)
            {
                lock (m_Lock)
                {
                    GetPhase(Operation == Operation.Database ? Phase.DatabasePatientsBindTags : Phase.ProjectPatientsBindTags)
                        .TagLookupRequests += count;
                }
            }

            public void RecordReferenceLookups(int count)
            {
                lock (m_Lock)
                {
                    GetPhase(LinkReferencesPhase(Operation)).ReferenceLookupRequests += count;
                }
            }

            public void RecordObjects(string family, int count)
            {
                lock (m_Lock)
                {
                    m_ObjectCounts.TryGetValue(family, out long current);
                    m_ObjectCounts[family] = current + count;
                }
            }

            public void MarkSucceeded()
            {
                lock (m_Lock)
                {
                    if (m_Status == "Running")
                    {
                        m_Status = "Succeeded";
                    }
                }
            }

            public void MarkCanceled()
            {
                lock (m_Lock)
                {
                    m_Status = "Canceled";
                }
            }

            public void MarkFailed(Exception exception)
            {
                lock (m_Lock)
                {
                    m_Status = "Failed";
                    m_FailureType = exception?.GetType().FullName ?? string.Empty;
                }
            }

            public void Finish()
            {
                long endCpuTicks = ProcessCpuTicks();
                lock (m_Lock)
                {
                    if (m_Status == "Running")
                    {
                        m_Status = "Incomplete";
                    }

                    m_EndedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                    m_TotalWallMilliseconds = ElapsedMilliseconds(m_StartTimestamp);
                    if (m_StartCpuTicks >= 0 && endCpuTicks >= m_StartCpuTicks)
                    {
                        m_TotalCpuMilliseconds = TimeSpan.FromTicks(endCpuTicks - m_StartCpuTicks).TotalMilliseconds;
                    }

                    m_EndManagedMemory = GC.GetTotalMemory(false);
                    m_Gc0 = GC.CollectionCount(0) - m_StartGc0;
                    m_Gc1 = GC.CollectionCount(1) - m_StartGc1;
                    m_Gc2 = GC.CollectionCount(2) - m_StartGc2;
                }
            }

            public LoadingSummary CreateSummary()
            {
                lock (m_Lock)
                {
                    return new LoadingSummary
                    {
                        schemaVersion = 1,
                        operation = Operation.ToString(),
                        runtime = Runtime,
                        scriptingBackend = ScriptingBackend,
                        platform = Platform,
                        unityVersion = UnityVersion,
                        startedUtc = StartedUtc,
                        endedUtc = m_EndedUtc,
                        status = m_Status,
                        failureType = m_FailureType,
                        totalWallMilliseconds = m_TotalWallMilliseconds,
                        totalCpuMilliseconds = m_TotalCpuMilliseconds,
                        managedMemoryBeforeBytes = m_StartManagedMemory,
                        managedMemoryAfterBytes = m_EndManagedMemory,
                        gc0Collections = m_Gc0,
                        gc1Collections = m_Gc1,
                        gc2Collections = m_Gc2,
                        phases = m_Phases
                            .OrderBy(pair => PhaseName(pair.Key), StringComparer.Ordinal)
                            .Select(pair => pair.Value.CreateSummary(pair.Key))
                            .ToList(),
                        objectCounts = m_ObjectCounts
                            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => new CountSummary { name = pair.Key, count = pair.Value })
                            .ToList()
                    };
                }
            }

            private PhaseAggregate GetPhase(Phase phase)
            {
                if (!m_Phases.TryGetValue(phase, out PhaseAggregate aggregate))
                {
                    aggregate = new PhaseAggregate();
                    m_Phases.Add(phase, aggregate);
                }

                return aggregate;
            }
        }

        private sealed class PhaseAggregate
        {
            public int Samples;
            public double CumulativeWallMilliseconds;
            public double MaximumWallMilliseconds;
            public double CumulativeCpuMilliseconds;
            public int CpuSamples;
            public long FileCount;
            public long ByteCount;
            public long ObjectCount;
            public int MaximumConcurrency;
            public long ManagedMemoryDeltaBytes;
            public long ManagedMemoryPeakBytes;
            public long AllocatedBytes;
            public int AllocationSamples;
            public int Gc0;
            public int Gc1;
            public int Gc2;
            public long TagLookupRequests;
            public long ReferenceLookupRequests;
            public long FileExistsCalls;
            public double FileExistsWallMilliseconds;

            public void Add(
                double wallMilliseconds,
                double cpuMilliseconds,
                int fileCount,
                long byteCount,
                int objectCount,
                int concurrency,
                long managedMemoryBefore,
                long managedMemoryAfter,
                long allocatedBytes,
                int gc0,
                int gc1,
                int gc2)
            {
                Samples++;
                CumulativeWallMilliseconds += wallMilliseconds;
                MaximumWallMilliseconds = Math.Max(MaximumWallMilliseconds, wallMilliseconds);
                if (cpuMilliseconds >= 0)
                {
                    CumulativeCpuMilliseconds += cpuMilliseconds;
                    CpuSamples++;
                }

                FileCount += fileCount;
                ByteCount += byteCount;
                ObjectCount += objectCount;
                MaximumConcurrency = Math.Max(MaximumConcurrency, concurrency);
                ManagedMemoryDeltaBytes += managedMemoryAfter - managedMemoryBefore;
                ManagedMemoryPeakBytes = Math.Max(ManagedMemoryPeakBytes, Math.Max(managedMemoryBefore, managedMemoryAfter));
                if (allocatedBytes >= 0)
                {
                    AllocatedBytes += allocatedBytes;
                    AllocationSamples++;
                }

                Gc0 += gc0;
                Gc1 += gc1;
                Gc2 += gc2;
            }

            public void AddFileExists(double wallMilliseconds)
            {
                FileExistsCalls++;
                FileExistsWallMilliseconds += wallMilliseconds;
            }

            public PhaseSummary CreateSummary(Phase phase)
            {
                return new PhaseSummary
                {
                    name = PhaseName(phase),
                    samples = Samples,
                    cumulativeWallMilliseconds = CumulativeWallMilliseconds,
                    maximumWallMilliseconds = MaximumWallMilliseconds,
                    cumulativeCpuMilliseconds = CpuSamples > 0 ? CumulativeCpuMilliseconds : -1,
                    cpuSamples = CpuSamples,
                    files = FileCount,
                    bytes = ByteCount,
                    rootObjects = ObjectCount,
                    maximumConcurrency = MaximumConcurrency,
                    managedMemoryDeltaBytes = ManagedMemoryDeltaBytes,
                    managedMemoryPeakBytes = ManagedMemoryPeakBytes,
                    allocatedBytes = AllocationSamples > 0 ? AllocatedBytes : -1,
                    allocationSamples = AllocationSamples,
                    gc0Collections = Gc0,
                    gc1Collections = Gc1,
                    gc2Collections = Gc2,
                    tagLookupRequests = TagLookupRequests,
                    referenceLookupRequests = ReferenceLookupRequests,
                    fileExistsCalls = FileExistsCalls,
                    fileExistsWallMilliseconds = FileExistsWallMilliseconds
                };
            }
        }

        [Serializable]
        internal sealed class LoadingSummary
        {
            public int schemaVersion;
            public string operation;
            public string runtime;
            public string scriptingBackend;
            public string platform;
            public string unityVersion;
            public string startedUtc;
            public string endedUtc;
            public string status;
            public string failureType;
            public double totalWallMilliseconds;
            public double totalCpuMilliseconds;
            public long managedMemoryBeforeBytes;
            public long managedMemoryAfterBytes;
            public int gc0Collections;
            public int gc1Collections;
            public int gc2Collections;
            public List<PhaseSummary> phases;
            public List<CountSummary> objectCounts;
        }

        [Serializable]
        internal sealed class PhaseSummary
        {
            public string name;
            public int samples;
            public double cumulativeWallMilliseconds;
            public double maximumWallMilliseconds;
            public double cumulativeCpuMilliseconds;
            public int cpuSamples;
            public long files;
            public long bytes;
            public long rootObjects;
            public int maximumConcurrency;
            public long managedMemoryDeltaBytes;
            public long managedMemoryPeakBytes;
            public long allocatedBytes;
            public int allocationSamples;
            public int gc0Collections;
            public int gc1Collections;
            public int gc2Collections;
            public long tagLookupRequests;
            public long referenceLookupRequests;
            public long fileExistsCalls;
            public double fileExistsWallMilliseconds;
        }

        [Serializable]
        internal sealed class CountSummary
        {
            public string name;
            public long count;
        }
    }
}
