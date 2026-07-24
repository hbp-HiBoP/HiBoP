using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HBP.Core.Tools
{
    public sealed class PatientAssetValidationResult
    {
        private readonly IReadOnlyList<MeshValidation> m_Meshes;
        private readonly IReadOnlyList<MRIValidation> m_MRIs;

        public long Generation { get; }
        public bool HasIssues =>
            m_Meshes.Any(validation => !validation.IsUsable) ||
            m_MRIs.Any(validation => !validation.IsUsable);

        internal PatientAssetValidationResult(
            long generation,
            IReadOnlyList<MeshValidation> meshes,
            IReadOnlyList<MRIValidation> MRIs)
        {
            Generation = generation;
            m_Meshes = meshes;
            m_MRIs = MRIs;
        }

        public bool TryApply(long currentGeneration)
        {
            if (Generation != currentGeneration)
            {
                return false;
            }

            foreach (MeshValidation validation in m_Meshes)
            {
                validation.Mesh.ApplyUsabilityValidation(validation.IsUsable);
            }
            foreach (MRIValidation validation in m_MRIs)
            {
                validation.MRI.ApplyUsabilityValidation(validation.IsUsable);
            }
            return true;
        }

        internal sealed class MeshValidation
        {
            public BaseMesh Mesh { get; }
            public bool IsUsable { get; }

            public MeshValidation(BaseMesh mesh, bool isUsable)
            {
                Mesh = mesh;
                IsUsable = isUsable;
            }
        }

        internal sealed class MRIValidation
        {
            public MRI MRI { get; }
            public bool IsUsable { get; }

            public MRIValidation(MRI MRI, bool isUsable)
            {
                this.MRI = MRI;
                IsUsable = isUsable;
            }
        }
    }

    /// <summary>
    /// Validates patient asset paths after JSON deserialization.
    /// </summary>
    public sealed class AssetReferenceValidator
    {
        private readonly Func<string, bool> m_FileExists;

        public AssetReferenceValidator() : this(LoadingDiagnostics.FileExists)
        {
        }

        public AssetReferenceValidator(Func<string, bool> fileExists)
        {
            m_FileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        }

        public async UniTask<PatientAssetValidationResult> ValidatePatientsAsync(
            IEnumerable<Patient> patients,
            int maxConcurrency,
            CancellationToken token,
            Action<int, int> updateProgress = null,
            long generation = 0)
        {
            if (patients == null)
            {
                throw new ArgumentNullException(nameof(patients));
            }

            token.ThrowIfCancellationRequested();
            Patient[] patientArray = patients.Where(patient => patient != null).ToArray();
            BaseMesh[] meshes = patientArray
                .Where(patient => patient.Meshes != null)
                .SelectMany(patient => patient.Meshes)
                .Where(mesh => mesh != null)
                .ToArray();
            MRI[] MRIs = patientArray
                .Where(patient => patient.MRIs != null)
                .SelectMany(patient => patient.MRIs)
                .Where(MRI => MRI != null)
                .ToArray();

            StringComparer pathComparer = Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            Dictionary<string, PathValidation> validationBySavedPath = new(pathComparer);
            Dictionary<string, PathValidation> validationByFullPath = new(pathComparer);

            foreach (BaseMesh mesh in meshes)
            {
                switch (mesh)
                {
                    case SingleMesh singleMesh:
                        RegisterPath(singleMesh.SavedPath, validationBySavedPath, validationByFullPath);
                        break;
                    case LeftRightMesh leftRightMesh:
                        RegisterPath(leftRightMesh.SavedLeftHemisphere, validationBySavedPath, validationByFullPath);
                        RegisterPath(leftRightMesh.SavedRightHemisphere, validationBySavedPath, validationByFullPath);
                        break;
                }
            }

            foreach (MRI MRI in MRIs)
            {
                RegisterPath(MRI.SavedFile, validationBySavedPath, validationByFullPath);
            }

            await ValidatePathsAsync(
                validationByFullPath.Values.ToArray(),
                maxConcurrency,
                token,
                updateProgress);
            token.ThrowIfCancellationRequested();

            PatientAssetValidationResult.MeshValidation[] meshResults = meshes
                .Select(mesh =>
                {
                    bool hasMesh = mesh switch
                    {
                        SingleMesh singleMesh => IsValid(
                        singleMesh.SavedPath,
                        BaseMesh.MESH_EXTENSION,
                        validationBySavedPath),
                        LeftRightMesh leftRightMesh =>
                            IsValid(
                            leftRightMesh.SavedLeftHemisphere,
                            BaseMesh.MESH_EXTENSION,
                            validationBySavedPath) &&
                            IsValid(
                            leftRightMesh.SavedRightHemisphere,
                            BaseMesh.MESH_EXTENSION,
                            validationBySavedPath),
                        _ => false
                    };
                    return new PatientAssetValidationResult.MeshValidation(
                        mesh,
                        !string.IsNullOrEmpty(mesh.Name) && hasMesh);
                })
                .ToArray();
            PatientAssetValidationResult.MRIValidation[] MRIResults = MRIs
                .Select(MRI => new PatientAssetValidationResult.MRIValidation(
                    MRI,
                    !string.IsNullOrEmpty(MRI.Name) &&
                    MRI.EXTENSIONS.Any(extension =>
                        IsValid(MRI.SavedFile, extension, validationBySavedPath))))
                .ToArray();

            return new PatientAssetValidationResult(generation, meshResults, MRIResults);
        }

        private static void RegisterPath(
            string savedPath,
            IDictionary<string, PathValidation> validationBySavedPath,
            IDictionary<string, PathValidation> validationByFullPath)
        {
            if (string.IsNullOrEmpty(savedPath) || validationBySavedPath.ContainsKey(savedPath))
            {
                return;
            }

            string fullPath = savedPath.ConvertToFullPath();
            if (!validationByFullPath.TryGetValue(fullPath, out PathValidation validation))
            {
                validation = new PathValidation(fullPath);
                validationByFullPath.Add(fullPath, validation);
            }
            validationBySavedPath.Add(savedPath, validation);
        }

        private async UniTask ValidatePathsAsync(
            PathValidation[] validations,
            int maxConcurrency,
            CancellationToken token,
            Action<int, int> updateProgress)
        {
            updateProgress?.Invoke(0, validations.Length);
            if (validations.Length == 0)
            {
                return;
            }

            int nextIndex = -1;
            int completedCount = 0;
            object progressLock = new();
            int workerCount = Math.Min(Math.Max(1, maxConcurrency), validations.Length);
            UniTask[] workers = Enumerable.Range(0, workerCount)
                .Select(_ => ValidateWorkerAsync(
                    validations,
                    () => Interlocked.Increment(ref nextIndex),
                    () =>
                    {
                        if (updateProgress == null)
                        {
                            return;
                        }

                        lock (progressLock)
                        {
                            updateProgress(++completedCount, validations.Length);
                        }
                    },
                    token))
                .ToArray();
            await UniTask.WhenAll(workers);
        }

        private async UniTask ValidateWorkerAsync(
            IReadOnlyList<PathValidation> validations,
            Func<int> nextIndex,
            Action pathValidated,
            CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int index = nextIndex();
                if (index >= validations.Count)
                {
                    return;
                }

                PathValidation validation = validations[index];
                validation.Exists = m_FileExists(validation.FullPath);
                pathValidated();
            }
        }

        private static bool IsValid(
            string savedPath,
            string extension,
            IReadOnlyDictionary<string, PathValidation> validationBySavedPath)
        {
            return !string.IsNullOrEmpty(savedPath) &&
                validationBySavedPath.TryGetValue(savedPath, out PathValidation validation) &&
                validation.Exists &&
                new FileInfo(validation.FullPath).Extension == extension;
        }

        private sealed class PathValidation
        {
            public string FullPath { get; }
            public bool Exists { get; set; }

            public PathValidation(string fullPath)
            {
                FullPath = fullPath;
            }
        }
    }
}
