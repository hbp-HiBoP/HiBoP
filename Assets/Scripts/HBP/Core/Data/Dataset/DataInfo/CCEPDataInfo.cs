using HBP.Core.Errors;
using HBP.Core.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class containing paths to CCEP data files.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the data.</description>
    /// </item>
    /// <item>
    /// <term><b>Patient</b></term>
    /// <description>Patient who has passed the experiment.</description>
    /// </item>
    /// <item>
    /// <term><b>Stimulated channel</b></term>
    /// <description>Stimulated channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Data container</b></term>
    /// <description>Data container containing all the paths to functional data files.</description>
    /// </item>
    /// <item>
    /// <term><b>Dataset</b></term>
    /// <description>Dataset the dataInfo belongs to.</description>
    /// </item>
    /// <item>
    /// <term><b>IsOk</b></term>
    /// <description>True if the dataInfo is visualizable, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>All dataInfo errors.</description>
    /// </item>
    /// <item>
    /// <term><b>OnRequestErrorCheck</b></term>
    /// <description>Callback executed when error checking is required.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("CCEP")]
    public class CCEPDataInfo : PatientDataInfo, IEpochable
    {
        #region Properties

        /// <summary>
        /// Stimulated channel.
        /// </summary>
        [JsonProperty] public string StimulatedChannel { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        /// <param name="id">Unique identifier</param>
        public CCEPDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string channel, string correspondingDatabaseID, string ID) : base(name, protocol, dataContainer, errors, warnings, patient, correspondingDatabaseID, ID)
        {
            StimulatedChannel = channel;
        }

        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        public CCEPDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string channel, string correspondingDatabaseID) : base(name, protocol, dataContainer, errors, warnings, patient, correspondingDatabaseID)
        {
            StimulatedChannel = channel;
        }

        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        public CCEPDataInfo() : this("Data", null, new Container.Elan(), new Error[0], new Warning[0], null, "Unknown", "")
        {
        }

        #endregion

        #region Operators

        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new CCEPDataInfo(Name, Protocol, DataContainer.Clone() as Container.DataContainer, Errors, Warnings, Patient, StimulatedChannel, CorrespondingDatabaseID, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is CCEPDataInfo ccepDataInfo)
            {
                StimulatedChannel = ccepDataInfo.StimulatedChannel;
            }
        }

        #endregion

        #region Private Methods

        internal override IEnumerable<ValidationState> GetValidationStates(ValidationAspect aspect, ValidationRequest request, DataInfoValidationContext context)
        {
            if (aspect == ValidationAspect.Epoching)
            {
                return GetEpochingValidationStates(request, context);
            }

            if (aspect == ValidationAspect.ChannelMapping)
            {
                bool exists = Patient != null && Patient.Sites.Any(site => site.Name == StimulatedChannel);
                return new[]
                {
                    CreateValidationState(aspect, string.Empty, $"{StimulatedChannel}|{GetSiteSignature()}", exists || Patient == null ? Array.Empty<Error>() : new Error[] { new ChannelNotFoundError() }, Array.Empty<Warning>())
                };
            }

            return base.GetValidationStates(aspect, request, context);
        }

        private IEnumerable<ValidationState> GetEpochingValidationStates(ValidationRequest request, DataInfoValidationContext context)
        {
            if (Protocol == null)
            {
                return new[]
                {
                    CreateNotApplicableState(ValidationAspect.Epoching)
                };
            }

            SubBloc[] subBlocs = Protocol.Blocs.SelectMany(bloc => bloc.SubBlocs).Where(subBloc => request.MatchesSubBloc(this, subBloc)).ToArray();
            if (subBlocs.Length == 0)
            {
                return new[]
                {
                    CreateNotApplicableState(ValidationAspect.Epoching)
                };
            }

            if (!subBlocs.Any(IsEpochable))
            {
                return subBlocs.Select(subBloc => CreateNotApplicableState(ValidationAspect.Epoching, subBloc.ID)).ToArray();
            }

            if (!context.TryGetEEGMetadata(out EEGValidationMetadata metadata, out Error error))
            {
                return new[]
                {
                    CreateValidationState(ValidationAspect.Epoching, string.Empty, context.SourceSignature, error == null ? Array.Empty<Error>() : new[] { error }, Array.Empty<Warning>())
                };
            }

            HashSet<int> triggerCodes = new(metadata.TriggerCodes);
            return subBlocs.Select(subBloc =>
            {
                if (!IsEpochable(subBloc))
                {
                    return CreateNotApplicableState(ValidationAspect.Epoching, subBloc.ID);
                }

                Event mainEvent = subBloc.MainEvent;
                bool found = mainEvent != null && mainEvent.Codes.Any(triggerCodes.Contains);
                return CreateValidationState(ValidationAspect.Epoching, subBloc.ID, GetEpochingSignature(context.SourceSignature, subBloc), found ? Array.Empty<Error>() : new Error[]
                {
                    new BlocsCantBeEpochedError($"{subBloc.Name} ({mainEvent?.Name ?? "no main event"})")
                }, Array.Empty<Warning>());
            }).ToArray();
        }

        private static bool IsEpochable(SubBloc subBloc)
        {
            return subBloc.Window.Length > 0 && subBloc.MainEvent != null && subBloc.MainEvent.Codes.Count > 0;
        }

        private static string GetEpochingSignature(string sourceSignature, SubBloc subBloc)
        {
            Event mainEvent = subBloc.MainEvent;
            string codes = mainEvent == null ? string.Empty : string.Join(",", mainEvent.Codes.Distinct().OrderBy(code => code));
            return $"{sourceSignature}|{subBloc.Type}|{mainEvent?.ID}|{codes}";
        }

        private string GetSiteSignature()
        {
            return Patient == null ? string.Empty : string.Join("|", Patient.Sites.Select(site => site.Name ?? string.Empty).OrderBy(name => name, StringComparer.Ordinal));
        }

        protected override IEnumerable<Error> GetErrors()
        {
            List<Error> errors = new(base.GetErrors());
            errors.AddRange(GetCCEPErrors());
            return errors;
        }

        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        private IEnumerable<Error> GetCCEPErrors()
        {
            List<Error> errors = new();
            if (m_DataContainer.IsOk)
            {
                DLL.EEG.File.FileType type;
                string[] files;
                if (m_DataContainer is Container.BrainVision brainVisionDataContainer)
                {
                    type = DLL.EEG.File.FileType.BrainVision;
                    files = new string[] { brainVisionDataContainer.Header };
                }
                else if (m_DataContainer is Container.EDF edfDataContainer)
                {
                    type = DLL.EEG.File.FileType.EDF;
                    files = new string[] { edfDataContainer.File };
                }
                else if (m_DataContainer is Container.Elan elanDataContainer)
                {
                    type = DLL.EEG.File.FileType.ELAN;
                    files = new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes };
                }
                else if (m_DataContainer is Container.Micromed micromedDataContainer)
                {
                    type = DLL.EEG.File.FileType.Micromed;
                    files = new string[] { micromedDataContainer.Path };
                }
                else if (m_DataContainer is Container.FIF fifDataContainer)
                {
                    type = DLL.EEG.File.FileType.FIF;
                    files = new string[] { fifDataContainer.File };
                }
                else
                {
                    throw new Exception("Invalid data container type");
                }

                using DLL.EEG.File file = new(type, false, files);
                List<DLL.EEG.Trigger> triggers = file.Triggers;
                if (Protocol.IsVisualizable && !Protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))))
                {
                    errors.Add(new BlocsCantBeEpochedError());
                }
            }

            if (!m_Patient.Sites.Any(site => site.Name == StimulatedChannel))
            {
                errors.Add(new ChannelNotFoundError());
            }

            return errors;
        }

        protected override IEnumerable<Warning> GetWarnings()
        {
            List<Warning> warnings = new(base.GetWarnings());
            warnings.AddRange(GetCCEPWarnings());
            return warnings;
        }

        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        private IEnumerable<Warning> GetCCEPWarnings()
        {
            List<Warning> warnings = new();
            return warnings;
        }

        #endregion
    }
}
