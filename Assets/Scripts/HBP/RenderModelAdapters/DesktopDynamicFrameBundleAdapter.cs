using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace HBP.RenderModelAdapters
{
    public static class DesktopDynamicFrameBundleAdapter
    {
        public static IReadOnlyList<DynamicColumnExpectation> CaptureExpectations(SessionSnapshot snapshot, ContractId visualizationId, IReadOnlyDictionary<ContractId, DynamicColumnContent> contentByColumn, IReadOnlyDictionary<ContractId, IReadOnlyList<ContractId>> cutIdsByColumn)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (!visualizationId.IsValid)
                throw new ArgumentException("A valid visualization ID is required.", nameof(visualizationId));
            if (contentByColumn == null || cutIdsByColumn == null)
                throw new ArgumentNullException(contentByColumn == null ? nameof(contentByColumn) : nameof(cutIdsByColumn));

            ScopeState visualization = FindEntityScope(snapshot.Scopes, ScopeType.Visualization, V1PropertyKeys.VisualizationEntity, visualizationId);
            IReadOnlyList<ContractId> membership = RequiredProperty(visualization, V1PropertyKeys.VisualizationColumnMembership, ContractValueKind.IdList).Ids;
            var result = new List<DynamicColumnExpectation>();
            for (int index = 0; index < membership.Count; index++)
            {
                ContractId columnId = membership[index];
                ScopeState column = FindEntityScope(snapshot.Scopes, ScopeType.Column, V1PropertyKeys.ColumnEntity, columnId);
                bool included = RequiredProperty(column, V1PropertyKeys.ColumnIncludedInTimeline, ContractValueKind.Boolean).Boolean;
                if (!included)
                    continue;
                if (!contentByColumn.TryGetValue(columnId, out DynamicColumnContent content))
                    throw new InvalidOperationException("Every timeline column requires an explicit dynamic content plan.");
                IReadOnlyList<ContractId> cutIds = cutIdsByColumn.TryGetValue(columnId, out IReadOnlyList<ContractId> configuredCuts) ? configuredCuts : Array.Empty<ContractId>();
                result.Add(new DynamicColumnExpectation(columnId, content, cutIds));
            }

            if (result.Count == 0)
                throw new InvalidOperationException("The visualization has no functional column included in its timeline.");
            return result.AsReadOnly();
        }

        public static DynamicFrameBundle CaptureBundle(SessionEpoch session, ContractId timelineId, ScopeRevision playbackRevision, ulong frameSequence, double logicalTime, RenderTemporalSample sample, StateRevision sourceStateRevision, IReadOnlyList<DynamicColumnExpectation> expectations, IEnumerable<ColumnFrame> frames)
        {
            return new DynamicFrameBundle(session, timelineId, playbackRevision, frameSequence, logicalTime, sample, sourceStateRevision, expectations, frames);
        }

        private static ScopeState FindEntityScope(IReadOnlyList<ScopeState> scopes, ScopeType type, PropertyKey entityKey, ContractId entityId)
        {
            ScopeState match = null;
            for (int index = 0; index < scopes.Count; index++)
            {
                ScopeState scope = scopes[index];
                if (scope.Scope.Type != type)
                    continue;
                ContractValue entity = RequiredProperty(scope, entityKey, ContractValueKind.Id);
                if (entity.Id != entityId)
                    continue;
                if (match != null)
                    throw new InvalidOperationException("An entity occurs in multiple canonical scopes.");
                match = scope;
            }

            return match ?? throw new InvalidOperationException("The requested entity is absent from the canonical snapshot.");
        }

        private static ContractValue RequiredProperty(ScopeState scope, PropertyKey key, ContractValueKind kind)
        {
            for (int index = 0; index < scope.Properties.Count; index++)
            {
                StateProperty property = scope.Properties[index];
                if (property.Key != key)
                    continue;
                if (property.Value.Kind != kind)
                    throw new InvalidOperationException("A canonical timeline property has the wrong value kind.");
                return property.Value;
            }

            throw new InvalidOperationException("A canonical timeline property is missing.");
        }
    }
}
