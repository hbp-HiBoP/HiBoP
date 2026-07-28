using HBP.Core.Errors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [Flags]
    public enum ValidationAspect
    {
        None = 0,
        Structure = 1 << 0,
        SourceAvailability = 1 << 1,
        SourceReadability = 1 << 2,
        StaticContent = 1 << 3,
        Epoching = 1 << 4,
        ChannelMapping = 1 << 5,
        PatientAssets = 1 << 6,

        DataInfoSemantic =
            Structure |
            SourceReadability |
            StaticContent |
            Epoching |
            ChannelMapping,
        DataInfoAll = SourceAvailability | DataInfoSemantic,
        Startup = SourceAvailability | PatientAssets,
        All = DataInfoAll | PatientAssets
    }

    public enum ValidationStatus
    {
        Current,
        Stale,
        Running,
        Failed,
        NotApplicable
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public sealed class ValidationState
    {
        [JsonProperty] public ValidationAspect Aspect { get; private set; }
        [JsonProperty] public string ScopeID { get; private set; }
        [JsonProperty] public ValidationStatus Status { get; private set; }
        [JsonProperty] public string Signature { get; private set; }
        [JsonProperty("Errors")] private Error[] m_Errors = Array.Empty<Error>();
        [JsonProperty("Warnings")] private Warning[] m_Warnings = Array.Empty<Warning>();

        [JsonIgnore] public IReadOnlyList<Error> Errors => m_Errors ?? Array.Empty<Error>();
        [JsonIgnore] public IReadOnlyList<Warning> Warnings => m_Warnings ?? Array.Empty<Warning>();

        [JsonConstructor]
        public ValidationState(
            ValidationAspect aspect,
            string scopeID,
            ValidationStatus status,
            string signature,
            Error[] errors,
            Warning[] warnings)
        {
            Aspect = aspect;
            ScopeID = scopeID ?? string.Empty;
            Status = status;
            Signature = signature ?? string.Empty;
            m_Errors = errors ?? Array.Empty<Error>();
            m_Warnings = warnings ?? Array.Empty<Warning>();
        }

        public ValidationState(
            ValidationAspect aspect,
            string scopeID,
            ValidationStatus status,
            string signature,
            IEnumerable<Error> errors,
            IEnumerable<Warning> warnings)
            : this(
                aspect,
                scopeID,
                status,
                signature,
                errors?.ToArray(),
                warnings?.ToArray())
        {
        }

        public ValidationState Clone()
        {
            return new ValidationState(
                Aspect,
                ScopeID,
                Status,
                Signature,
                Errors,
                Warnings);
        }

        public ValidationState WithStatus(ValidationStatus status)
        {
            return new ValidationState(
                Aspect,
                ScopeID,
                status,
                Signature,
                Errors,
                Warnings);
        }
    }

    public sealed class ValidationRequest
    {
        private sealed class Scope
        {
            public ValidationAspect Aspects { get; }
            public HashSet<string> DataInfoIDs { get; }
            public HashSet<string> PatientIDs { get; }
            public HashSet<string> ProtocolIDs { get; }
            public HashSet<string> SubBlocIDs { get; }

            public Scope(
                ValidationAspect aspects,
                IEnumerable<string> dataInfoIDs,
                IEnumerable<string> patientIDs,
                IEnumerable<string> protocolIDs,
                IEnumerable<string> subBlocIDs)
            {
                Aspects = aspects;
                DataInfoIDs = CreateSet(dataInfoIDs);
                PatientIDs = CreateSet(patientIDs);
                ProtocolIDs = CreateSet(protocolIDs);
                SubBlocIDs = CreateSet(subBlocIDs);
            }
        }

        private readonly IReadOnlyList<Scope> m_Scopes;
        private readonly HashSet<string> m_DataInfoIDs;
        private readonly HashSet<string> m_PatientIDs;
        private readonly HashSet<string> m_ProtocolIDs;
        private readonly HashSet<string> m_SubBlocIDs;

        public ValidationAspect Aspects { get; }
        public bool Force { get; }
        public IReadOnlyCollection<string> DataInfoIDs => m_DataInfoIDs;
        public IReadOnlyCollection<string> PatientIDs => m_PatientIDs;
        public IReadOnlyCollection<string> ProtocolIDs => m_ProtocolIDs;
        public IReadOnlyCollection<string> SubBlocIDs => m_SubBlocIDs;

        public static ValidationRequest Startup { get; } =
            new(ValidationAspect.Startup, force: true);
        public static ValidationRequest Full { get; } =
            new(ValidationAspect.All, force: true);
        public static ValidationRequest FullDataInfo { get; } =
            new(ValidationAspect.DataInfoAll, force: true);

        public ValidationRequest(
            ValidationAspect aspects,
            IEnumerable<string> dataInfoIDs = null,
            IEnumerable<string> patientIDs = null,
            IEnumerable<string> protocolIDs = null,
            IEnumerable<string> subBlocIDs = null,
            bool force = false)
        {
            Aspects = aspects;
            Force = force;
            m_DataInfoIDs = CreateSet(dataInfoIDs);
            m_PatientIDs = CreateSet(patientIDs);
            m_ProtocolIDs = CreateSet(protocolIDs);
            m_SubBlocIDs = CreateSet(subBlocIDs);
            m_Scopes = new[]
            {
                new Scope(
                    aspects,
                    m_DataInfoIDs,
                    m_PatientIDs,
                    m_ProtocolIDs,
                    m_SubBlocIDs)
            };
        }

        private ValidationRequest(
            IEnumerable<Scope> scopes,
            bool force)
        {
            m_Scopes = scopes
                .Where(scope => scope.Aspects != ValidationAspect.None)
                .ToArray();
            Aspects = m_Scopes.Aggregate(
                ValidationAspect.None,
                (aspects, scope) => aspects | scope.Aspects);
            Force = force;
            m_DataInfoIDs = CreateSet(
                m_Scopes.SelectMany(scope => scope.DataInfoIDs));
            m_PatientIDs = CreateSet(
                m_Scopes.SelectMany(scope => scope.PatientIDs));
            m_ProtocolIDs = CreateSet(
                m_Scopes.SelectMany(scope => scope.ProtocolIDs));
            m_SubBlocIDs = CreateSet(
                m_Scopes.SelectMany(scope => scope.SubBlocIDs));
        }

        public bool Includes(ValidationAspect aspect)
        {
            return (Aspects & aspect) != 0;
        }

        public bool Matches(DataInfo dataInfo)
        {
            return dataInfo != null &&
                m_Scopes.Any(scope =>
                    (scope.Aspects & ValidationAspect.DataInfoAll) != 0 &&
                    Matches(scope, dataInfo));
        }

        public bool Matches(
            DataInfo dataInfo,
            ValidationAspect aspect)
        {
            return dataInfo != null &&
                m_Scopes.Any(scope =>
                    (scope.Aspects & aspect) != 0 &&
                    Matches(scope, dataInfo));
        }

        public bool Matches(Patient patient)
        {
            return patient != null &&
                m_Scopes.Any(scope =>
                    (scope.Aspects & ValidationAspect.PatientAssets) != 0 &&
                    (scope.PatientIDs.Count == 0 ||
                        scope.PatientIDs.Contains(patient.ID)));
        }

        public bool MatchesSubBloc(
            DataInfo dataInfo,
            SubBloc subBloc)
        {
            return dataInfo != null &&
                subBloc != null &&
                m_Scopes.Any(scope =>
                    (scope.Aspects & ValidationAspect.Epoching) != 0 &&
                    Matches(scope, dataInfo) &&
                    (scope.SubBlocIDs.Count == 0 ||
                        scope.SubBlocIDs.Contains(subBloc.ID)));
        }

        public IReadOnlyCollection<string> GetTargetedSubBlocIDs(
            DataInfo dataInfo)
        {
            Scope[] scopes = m_Scopes
                .Where(scope =>
                    (scope.Aspects & ValidationAspect.Epoching) != 0 &&
                    Matches(scope, dataInfo))
                .ToArray();
            if (scopes.Length == 0 ||
                scopes.Any(scope => scope.SubBlocIDs.Count == 0))
            {
                return Array.Empty<string>();
            }
            return scopes
                .SelectMany(scope => scope.SubBlocIDs)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public ValidationRequest Merge(ValidationRequest other)
        {
            if (other == null)
            {
                return this;
            }
            if (Aspects == ValidationAspect.None)
            {
                return other;
            }
            if (other.Aspects == ValidationAspect.None)
            {
                return this;
            }
            return new ValidationRequest(
                m_Scopes.Concat(other.m_Scopes),
                Force || other.Force);
        }

        private static HashSet<string> CreateSet(IEnumerable<string> values)
        {
            return new HashSet<string>(
                values?.Where(value => !string.IsNullOrEmpty(value)) ??
                    Enumerable.Empty<string>(),
                StringComparer.Ordinal);
        }

        private static bool Matches(
            Scope scope,
            DataInfo dataInfo)
        {
            if (scope.DataInfoIDs.Count > 0 &&
                !scope.DataInfoIDs.Contains(dataInfo.ID))
            {
                return false;
            }
            if (scope.ProtocolIDs.Count > 0 &&
                (dataInfo.Protocol == null ||
                    !scope.ProtocolIDs.Contains(dataInfo.Protocol.ID)))
            {
                return false;
            }
            if (scope.PatientIDs.Count > 0 &&
                (dataInfo is not PatientDataInfo patientDataInfo ||
                    patientDataInfo.Patient == null ||
                    !scope.PatientIDs.Contains(patientDataInfo.Patient.ID)))
            {
                return false;
            }
            return true;
        }
    }
}
