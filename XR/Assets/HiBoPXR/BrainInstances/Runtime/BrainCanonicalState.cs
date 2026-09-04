using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.StaticRendering;
using CRNL.HiBoP.XR.Sites;

namespace CRNL.HiBoP.XR.BrainInstances
{
    internal sealed class BrainCanonicalState
    {
        private readonly Dictionary<ContractId, VisualizationState> m_Visualizations;
        private readonly Dictionary<ContractId, ColumnState> m_Columns;

        private BrainCanonicalState(SessionEpoch session, StateRevision stateRevision, Dictionary<ContractId, VisualizationState> visualizations, Dictionary<ContractId, ColumnState> columns)
        {
            Session = session;
            StateRevision = stateRevision;
            m_Visualizations = visualizations;
            m_Columns = columns;
        }

        public SessionEpoch Session { get; }

        public StateRevision StateRevision { get; }

        public static BrainCanonicalState FromSnapshot(SessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Dictionary<ContractId, AssetHash> assetsById = IndexAssets(snapshot.Assets);
            Dictionary<ContractId, ColumnState> columns = IndexColumns(snapshot.Scopes);
            Dictionary<ContractId, VisualizationState> visualizations = IndexVisualizations(snapshot.Scopes, assetsById, columns);
            ValidateProjectMembership(snapshot.Scopes, visualizations);
            return new BrainCanonicalState(snapshot.Session, snapshot.StateRevision, visualizations, columns);
        }

        public bool TryGetSiteSelectionContext(ContractId columnId, out SiteSelectionContext context)
        {
            if (columnId.IsValid && m_Columns.TryGetValue(columnId, out ColumnState column))
            {
                context = new SiteSelectionContext(Session, columnId, column.Scope, StateRevision, column.ScopeRevision);
                return true;
            }

            context = default;
            return false;
        }

        public bool TryResolve(BrainInstanceBinding binding, out ResolvedBrainBinding resolved)
        {
            if (!binding.IsValid || !m_Visualizations.TryGetValue(binding.VisualizationId, out VisualizationState visualization))
            {
                resolved = default;
                return false;
            }

            ContractId activeColumnId = default;
            if (binding.Kind == BrainBindingKind.ColumnBound)
            {
                if (!visualization.Columns.Contains(binding.ColumnId))
                {
                    resolved = default;
                    return false;
                }

                activeColumnId = binding.ColumnId;
            }
            else if (binding.Kind == BrainBindingKind.VisualizationBound)
            {
                activeColumnId = visualization.SelectedColumnId;
            }
            else
            {
                resolved = default;
                return false;
            }

            resolved = new ResolvedBrainBinding(visualization.SurfaceHash, visualization.Representation, visualization.Transparency, activeColumnId);
            return true;
        }

        public BrainInstanceCloseReason GetInvalidationReason(BrainInstanceBinding binding)
        {
            if (!m_Visualizations.TryGetValue(binding.VisualizationId, out VisualizationState visualization))
                return BrainInstanceCloseReason.VisualizationClosed;
            if (binding.Kind == BrainBindingKind.ColumnBound && !visualization.Columns.Contains(binding.ColumnId))
                return BrainInstanceCloseReason.ColumnClosed;
            return BrainInstanceCloseReason.Unknown;
        }

        private static Dictionary<ContractId, AssetHash> IndexAssets(IReadOnlyList<AssetReference> assets)
        {
            Dictionary<ContractId, AssetHash> result = new();
            for (int index = 0; index < assets.Count; index++)
            {
                AssetReference asset = assets[index];
                if (!result.TryAdd(asset.AssetId, asset.Hash))
                    throw new InvalidOperationException("A snapshot maps one asset ID to multiple hashes.");
            }

            return result;
        }

        private static Dictionary<ContractId, ColumnState> IndexColumns(IReadOnlyList<ScopeState> scopes)
        {
            Dictionary<ContractId, ColumnState> result = new();
            for (int index = 0; index < scopes.Count; index++)
            {
                ScopeState scope = scopes[index];
                if (scope.Scope.Type != ScopeType.Column)
                    continue;

                ContractId columnId = RequiredProperty(scope, V1PropertyKeys.ColumnEntity, ContractValueKind.Id).Id;
                ContractId visualizationId = RequiredProperty(scope, V1PropertyKeys.ColumnVisualization, ContractValueKind.Id).Id;
                bool selected = RequiredProperty(scope, V1PropertyKeys.ColumnSelected, ContractValueKind.Boolean).Boolean;
                if (!result.TryAdd(columnId, new ColumnState(visualizationId, selected, scope.Scope, scope.Revision)))
                    throw new InvalidOperationException("A column entity occurs in multiple scopes.");
            }

            return result;
        }

        private static Dictionary<ContractId, VisualizationState> IndexVisualizations(IReadOnlyList<ScopeState> scopes, IReadOnlyDictionary<ContractId, AssetHash> assetsById, IReadOnlyDictionary<ContractId, ColumnState> columns)
        {
            Dictionary<ContractId, VisualizationState> result = new();
            for (int index = 0; index < scopes.Count; index++)
            {
                ScopeState scope = scopes[index];
                if (scope.Scope.Type != ScopeType.Visualization)
                    continue;

                ContractId visualizationId = RequiredProperty(scope, V1PropertyKeys.VisualizationEntity, ContractValueKind.Id).Id;
                ContractId assetId = RequiredProperty(scope, V1PropertyKeys.VisualizationSurfaceAsset, ContractValueKind.Id).Id;
                if (!assetsById.TryGetValue(assetId, out AssetHash surfaceHash))
                    throw new InvalidOperationException("A visualization refers to an asset ID absent from the snapshot inventory.");

                ulong representationValue = RequiredProperty(scope, V1PropertyKeys.VisualizationSurfaceRepresentation, ContractValueKind.UnsignedInteger).UnsignedInteger;
                if (representationValue < (ulong)SurfaceRepresentation.Anatomical || representationValue > (ulong)SurfaceRepresentation.Other)
                    throw new InvalidOperationException("A visualization contains an unsupported surface representation.");

                bool transparent = RequiredProperty(scope, V1PropertyKeys.VisualizationTransparentBrain, ContractValueKind.Boolean).Boolean;
                IReadOnlyList<ContractId> membership = RequiredProperty(scope, V1PropertyKeys.VisualizationColumnMembership, ContractValueKind.IdList).Ids;
                HashSet<ContractId> columnIds = new();
                ContractId selectedColumnId = default;
                for (int memberIndex = 0; memberIndex < membership.Count; memberIndex++)
                {
                    ContractId columnId = membership[memberIndex];
                    if (!columnIds.Add(columnId))
                        throw new InvalidOperationException("A visualization column membership contains a duplicate.");
                    if (!columns.TryGetValue(columnId, out ColumnState column) || column.VisualizationId != visualizationId)
                        throw new InvalidOperationException("A visualization membership does not match the column parent mapping.");
                    if (column.Selected)
                    {
                        if (selectedColumnId.IsValid)
                            throw new InvalidOperationException("A visualization cannot have multiple selected columns.");
                        selectedColumnId = columnId;
                    }
                }

                var state = new VisualizationState(surfaceHash, (SurfaceRepresentation)representationValue, transparent ? SurfaceTransparency.Transparent : SurfaceTransparency.Opaque, columnIds, selectedColumnId);
                if (!result.TryAdd(visualizationId, state))
                    throw new InvalidOperationException("A visualization entity occurs in multiple scopes.");
            }

            return result;
        }

        private static void ValidateProjectMembership(IReadOnlyList<ScopeState> scopes, IReadOnlyDictionary<ContractId, VisualizationState> visualizations)
        {
            HashSet<ContractId> membership = null;
            for (int index = 0; index < scopes.Count; index++)
            {
                ScopeState scope = scopes[index];
                if (scope.Scope.Type != ScopeType.Project)
                    continue;
                if (membership != null)
                    throw new InvalidOperationException("P09 requires exactly one project scope.");
                membership = new HashSet<ContractId>(RequiredProperty(scope, V1PropertyKeys.ProjectVisualizationMembership, ContractValueKind.IdList).Ids);
            }

            if (membership == null)
                throw new InvalidOperationException("P09 requires a project visualization membership.");
            if (membership.Count != visualizations.Count)
                throw new InvalidOperationException("The project membership and visualization scopes differ.");
            foreach (ContractId visualizationId in membership)
            {
                if (!visualizations.ContainsKey(visualizationId))
                    throw new InvalidOperationException("The project membership names a visualization without a mapped scope.");
            }
        }

        private static ContractValue RequiredProperty(ScopeState scope, PropertyKey key, ContractValueKind kind)
        {
            for (int index = 0; index < scope.Properties.Count; index++)
            {
                StateProperty property = scope.Properties[index];
                if (property.Key != key)
                    continue;
                if (property.Value.Kind != kind)
                    throw new InvalidOperationException($"Property {key.Value} has the wrong value kind.");
                return property.Value;
            }

            throw new InvalidOperationException($"Scope {scope.Scope} is missing required property {key.Value}.");
        }

        private readonly struct ColumnState
        {
            public ColumnState(ContractId visualizationId, bool selected, ScopeKey scope, ScopeRevision scopeRevision)
            {
                VisualizationId = visualizationId;
                Selected = selected;
                Scope = scope;
                ScopeRevision = scopeRevision;
            }

            public ContractId VisualizationId { get; }

            public bool Selected { get; }

            public ScopeKey Scope { get; }

            public ScopeRevision ScopeRevision { get; }
        }

        private sealed class VisualizationState
        {
            public VisualizationState(AssetHash surfaceHash, SurfaceRepresentation representation, SurfaceTransparency transparency, HashSet<ContractId> columns, ContractId selectedColumnId)
            {
                SurfaceHash = surfaceHash;
                Representation = representation;
                Transparency = transparency;
                Columns = columns;
                SelectedColumnId = selectedColumnId;
            }

            public AssetHash SurfaceHash { get; }

            public SurfaceRepresentation Representation { get; }

            public SurfaceTransparency Transparency { get; }

            public HashSet<ContractId> Columns { get; }

            public ContractId SelectedColumnId { get; }
        }
    }
}
