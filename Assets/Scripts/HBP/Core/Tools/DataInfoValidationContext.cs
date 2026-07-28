using HBP.Core.Errors;
using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HBP.Core.Data
{
    public sealed class EEGValidationMetadata
    {
        public IReadOnlyCollection<int> TriggerCodes { get; }
        public IReadOnlyCollection<string> ChannelLabels { get; }

        public EEGValidationMetadata(
            IEnumerable<int> triggerCodes,
            IEnumerable<string> channelLabels)
        {
            TriggerCodes = triggerCodes.Distinct().ToArray();
            ChannelLabels = channelLabels
                .Where(label => !string.IsNullOrEmpty(label))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }

    public interface IEEGValidationMetadataReader
    {
        EEGValidationMetadata Read(DataInfo dataInfo);
    }

    internal sealed class DataInfoValidationContext
    {
        private readonly DataInfo m_DataInfo;
        private readonly IEEGValidationMetadataReader m_MetadataReader;
        private bool m_MetadataRead;
        private EEGValidationMetadata m_Metadata;
        private Error m_MetadataError;

        public string SourceSignature { get; }

        public DataInfoValidationContext(
            DataInfo dataInfo,
            IEEGValidationMetadataReader metadataReader = null)
        {
            m_DataInfo = dataInfo ?? throw new ArgumentNullException(nameof(dataInfo));
            m_MetadataReader = metadataReader;
            SourceSignature = BuildSourceSignature(dataInfo);
        }

        internal static string GetSourceSignature(DataInfo dataInfo)
        {
            return dataInfo == null
                ? string.Empty
                : BuildSourceSignature(dataInfo);
        }

        internal static string GetSourceDefinitionSignature(
            DataInfo dataInfo)
        {
            if (dataInfo == null)
            {
                return string.Empty;
            }
            string maskType = dataInfo switch
            {
                FMRIDataInfo fmri =>
                    fmri.MaskDataContainer?.GetType().FullName,
                MEGvDataInfo megv =>
                    megv.MaskDataContainer?.GetType().FullName,
                SharedFMRIDataInfo shared =>
                    shared.MaskDataContainer?.GetType().FullName,
                _ => string.Empty
            };
            return string.Join(
                "|",
                new[]
                {
                    dataInfo.DataContainer?.GetType().FullName ??
                        string.Empty,
                    maskType ?? string.Empty
                }.Concat(
                    GetSavedSourcePaths(dataInfo)
                        .Select(path =>
                            path?.ConvertToFullPath() ??
                                string.Empty)
                        .OrderBy(
                            path => path,
                            StringComparer.OrdinalIgnoreCase)));
        }

        internal static IEnumerable<string> GetSavedSourcePaths(
            DataInfo dataInfo)
        {
            if (dataInfo == null)
            {
                return Array.Empty<string>();
            }
            IEnumerable<string> primary = dataInfo.DataContainer switch
            {
                Container.BrainVision brainVision =>
                    new[] { brainVision.SavedHeader },
                Container.EDF edf =>
                    new[] { edf.SavedFile },
                Container.Elan elan =>
                    new[] { elan.SavedEEG, elan.SavedPOS },
                Container.Micromed micromed =>
                    new[] { micromed.SavedPath },
                Container.FIF fif =>
                    new[] { fif.SavedFile },
                Container.CSV csv =>
                    new[] { csv.SavedFile },
                Container.Nifti nifti =>
                    new[] { nifti.SavedFile },
                _ =>
                    Array.Empty<string>()
            };
            string mask = dataInfo switch
            {
                FMRIDataInfo fmri =>
                    fmri.MaskDataContainer?.SavedFile,
                MEGvDataInfo megv =>
                    megv.MaskDataContainer?.SavedFile,
                SharedFMRIDataInfo shared =>
                    shared.MaskDataContainer?.SavedFile,
                _ => null
            };
            return string.IsNullOrEmpty(mask)
                ? primary
                : primary.Concat(new[] { mask });
        }

        public bool TryGetEEGMetadata(
            out EEGValidationMetadata metadata,
            out Error error)
        {
            if (!m_MetadataRead)
            {
                ReadMetadata();
            }
            metadata = m_Metadata;
            error = m_MetadataError;
            return metadata != null;
        }

        private void ReadMetadata()
        {
            m_MetadataRead = true;
            try
            {
                if (m_MetadataReader != null)
                {
                    m_Metadata = m_MetadataReader.Read(m_DataInfo);
                    return;
                }

                EEGRecordingSource source = EEGRecordingSource.From(m_DataInfo);
                string[] missingFiles = GetSourcePaths(m_DataInfo)
                    .Where(path => !string.IsNullOrWhiteSpace(path) && !File.Exists(path))
                    .ToArray();
                if (missingFiles.Length > 0)
                {
                    m_MetadataError = new FileDoesNotExistError(
                        string.Join(", ", missingFiles));
                    return;
                }

                using DLL.EEG.File file = new(
                    source.FileType,
                    false,
                    source.ReaderFiles);
                if (file.getHandle().Handle == IntPtr.Zero)
                {
                    m_MetadataError = new SourceUnreadableError(
                        "The native EEG reader could not open the source.");
                    return;
                }
                m_Metadata = new EEGValidationMetadata(
                    file.Triggers.Select(trigger => trigger.Code),
                    file.Electrodes.Select(electrode => electrode.Label));
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is InvalidDataException ||
                exception is SEHException)
            {
                m_MetadataError = new SourceUnreadableError(exception.Message);
            }
        }

        private static string BuildSourceSignature(DataInfo dataInfo)
        {
            IEnumerable<string> paths = GetSourcePaths(dataInfo);
            StringBuilder signature = new();
            foreach (string path in paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                signature.Append(GetPathSignature(path));
                signature.Append('|');
            }
            return signature.ToString();
        }

        private static IEnumerable<string> GetSourcePaths(DataInfo dataInfo)
        {
            IEnumerable<string> primary = dataInfo.DataContainer switch
            {
                Container.BrainVision brainVision =>
                    new[] { brainVision.Header }
                        .Concat(EEGRecordingSource.GetBrainVisionReferencedFiles(
                            brainVision.Header)),
                Container.EDF edf =>
                    new[] { edf.File },
                Container.Elan elan =>
                    new[] { elan.EEG, elan.EEGHeader, elan.POS },
                Container.Micromed micromed =>
                    new[] { micromed.Path },
                Container.FIF fif =>
                    new[] { fif.File },
                Container.CSV csv =>
                    new[] { csv.File },
                Container.Nifti nifti =>
                    new[] { nifti.File },
                _ =>
                    Array.Empty<string>()
            };

            string mask = dataInfo switch
            {
                FMRIDataInfo fmri => fmri.MaskDataContainer?.File,
                MEGvDataInfo megv => megv.MaskDataContainer?.File,
                SharedFMRIDataInfo shared =>
                    shared.MaskDataContainer?.File,
                _ => null
            };
            return string.IsNullOrEmpty(mask)
                ? primary
                : primary.Concat(new[] { mask });
        }

        private static string GetPathSignature(string path)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                FileInfo file = new(fullPath);
                return file.Exists
                    ? $"{fullPath}:{file.Length}:{file.LastWriteTimeUtc.Ticks}"
                    : $"{fullPath}:missing";
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException ||
                exception is IOException ||
                exception is UnauthorizedAccessException)
            {
                return $"{path}:unavailable";
            }
        }
    }
}
