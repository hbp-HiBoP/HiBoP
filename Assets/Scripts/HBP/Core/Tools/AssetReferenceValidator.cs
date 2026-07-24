using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HBP.Core.Tools
{
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

        public async UniTask ValidatePatientsAsync(
            IEnumerable<Patient> patients,
            int maxConcurrency,
            CancellationToken token,
            Action<int, int> updateProgress = null)
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

            foreach (BaseMesh mesh in meshes)
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
                mesh.ApplyUsabilityValidation(!string.IsNullOrEmpty(mesh.Name) && hasMesh);
            }

            foreach (MRI MRI in MRIs)
            {
                bool hasMRI = MRI.EXTENSIONS.Any(extension =>
                    IsValid(MRI.SavedFile, extension, validationBySavedPath));
                MRI.ApplyUsabilityValidation(!string.IsNullOrEmpty(MRI.Name) && hasMRI);
            }
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
