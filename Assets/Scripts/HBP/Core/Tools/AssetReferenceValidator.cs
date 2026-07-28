using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Errors;
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
        private readonly IReadOnlyList<PatientValidation> m_Patients;

        public long Generation { get; }
        public bool HasIssues =>
            m_Meshes.Any(validation => !validation.IsUsable) ||
            m_MRIs.Any(validation => !validation.IsUsable);

        internal PatientAssetValidationResult(
            long generation,
            IReadOnlyList<MeshValidation> meshes,
            IReadOnlyList<MRIValidation> MRIs,
            IReadOnlyList<PatientValidation> patients)
        {
            Generation = generation;
            m_Meshes = meshes;
            m_MRIs = MRIs;
            m_Patients = patients;
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
            foreach (PatientValidation validation in m_Patients)
            {
                validation.Patient.ApplyAssetValidationState(
                    validation.State);
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

        internal sealed class PatientValidation
        {
            public Patient Patient { get; }
            public ValidationState State { get; }

            public PatientValidation(
                Patient patient,
                ValidationState state)
            {
                Patient = patient;
                State = state;
            }
        }
    }

    /// <summary>
    /// Validates patient asset paths after JSON deserialization.
    /// </summary>
    public sealed class AssetReferenceValidator
    {
        private readonly Func<string, bool> m_FileExists;

        public AssetReferenceValidator() : this(File.Exists)
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
            long generation = 0,
            Func<LoadingWorkPriority> priorityProvider = null)
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
                updateProgress,
                priorityProvider);
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

            PatientAssetValidationResult.PatientValidation[] patientResults =
                patientArray
                    .Select(patient =>
                    {
                        string[] savedPaths = GetSavedPaths(patient)
                            .Where(path => !string.IsNullOrEmpty(path))
                            .Distinct(pathComparer)
                            .OrderBy(path => path, pathComparer)
                            .ToArray();
                        Error[] errors = GetAssetErrors(
                            patient,
                            validationBySavedPath);
                        string signature = string.Join(
                            "|",
                            savedPaths.Select(path =>
                                validationBySavedPath.TryGetValue(
                                    path,
                                    out PathValidation validation)
                                    ? validation.Signature
                                    : $"{path}:missing"));
                        return new PatientAssetValidationResult.PatientValidation(
                            patient,
                            new ValidationState(
                                ValidationAspect.PatientAssets,
                                patient.ID,
                                ValidationStatus.Current,
                                signature,
                                errors,
                                Array.Empty<Warning>()));
                    })
                    .ToArray();

            return new PatientAssetValidationResult(
                generation,
                meshResults,
                MRIResults,
                patientResults);
        }

        private static IEnumerable<string> GetSavedPaths(
            Patient patient)
        {
            IEnumerable<string> meshPaths = patient.Meshes.SelectMany(mesh =>
                mesh switch
                {
                    SingleMesh single =>
                        new[] { single.SavedPath },
                    LeftRightMesh leftRight =>
                        new[]
                        {
                            leftRight.SavedLeftHemisphere,
                            leftRight.SavedRightHemisphere
                        },
                    _ => Array.Empty<string>()
                });
            return meshPaths.Concat(
                patient.MRIs.Select(MRI => MRI.SavedFile));
        }

        private static Error[] GetAssetErrors(
            Patient patient,
            IReadOnlyDictionary<string, PathValidation>
                validationBySavedPath)
        {
            List<Error> errors = new();
            foreach (BaseMesh mesh in patient.Meshes)
            {
                if (string.IsNullOrEmpty(mesh.Name))
                {
                    errors.Add(new RequiredFieldEmptyError(
                        "Mesh name is empty"));
                }
                switch (mesh)
                {
                    case SingleMesh single:
                        AddPathError(
                            single.SavedPath,
                            new[] { BaseMesh.MESH_EXTENSION },
                            "Mesh",
                            validationBySavedPath,
                            errors);
                        break;
                    case LeftRightMesh leftRight:
                        AddPathError(
                            leftRight.SavedLeftHemisphere,
                            new[] { BaseMesh.MESH_EXTENSION },
                            "Left mesh",
                            validationBySavedPath,
                            errors);
                        AddPathError(
                            leftRight.SavedRightHemisphere,
                            new[] { BaseMesh.MESH_EXTENSION },
                            "Right mesh",
                            validationBySavedPath,
                            errors);
                        break;
                }
            }
            foreach (MRI MRI in patient.MRIs)
            {
                if (string.IsNullOrEmpty(MRI.Name))
                {
                    errors.Add(new RequiredFieldEmptyError(
                        "MRI name is empty"));
                }
                AddPathError(
                    MRI.SavedFile,
                    MRI.EXTENSIONS,
                    "MRI",
                    validationBySavedPath,
                    errors);
            }
            return errors.ToArray();
        }

        private static void AddPathError(
            string savedPath,
            IEnumerable<string> extensions,
            string label,
            IReadOnlyDictionary<string, PathValidation>
                validationBySavedPath,
            ICollection<Error> errors)
        {
            if (string.IsNullOrEmpty(savedPath))
            {
                errors.Add(new RequiredFieldEmptyError(
                    $"{label} path is empty"));
                return;
            }
            if (!validationBySavedPath.TryGetValue(
                    savedPath,
                    out PathValidation validation) ||
                !validation.Exists)
            {
                errors.Add(new FileDoesNotExistError(savedPath));
                return;
            }
            string extension =
                new FileInfo(validation.FullPath).Extension;
            if (!extensions.Any(expected =>
                string.Equals(
                    extension,
                    expected,
                    StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new WrongExtensionError(
                    $"{label} has a wrong extension: {savedPath}"));
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
            Action<int, int> updateProgress,
            Func<LoadingWorkPriority> priorityProvider)
        {
            Func<UniTask<bool>>[] tasks = validations
                .Select(validation => (Func<UniTask<bool>>)(async () =>
                {
                    await UniTask.SwitchToThreadPool();
                    ValidatePath(validation);
                    return true;
                }))
                .ToArray();
            await LoadingWorkScheduler.Shared.RunAsync(
                tasks,
                LoadingWorkCategory.FileSystem,
                priorityProvider,
                token,
                updateProgress,
                maxConcurrency);
        }

        private void ValidatePath(PathValidation validation)
        {
            validation.Exists = m_FileExists(validation.FullPath);
            if (validation.Exists)
            {
                try
                {
                    FileInfo file = new(validation.FullPath);
                    if (file.Exists)
                    {
                        validation.Length = file.Length;
                        validation.LastWriteTimeUtcTicks =
                            file.LastWriteTimeUtc.Ticks;
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
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
                string.Equals(
                    new FileInfo(validation.FullPath).Extension,
                    extension,
                    StringComparison.OrdinalIgnoreCase);
        }

        private sealed class PathValidation
        {
            public string FullPath { get; }
            public bool Exists { get; set; }
            public long Length { get; set; }
            public long LastWriteTimeUtcTicks { get; set; }
            public string Signature => Exists
                ? $"{FullPath}:{Length}:{LastWriteTimeUtcTicks}"
                : $"{FullPath}:missing";

            public PathValidation(string fullPath)
            {
                FullPath = fullPath;
            }
        }
    }
}
