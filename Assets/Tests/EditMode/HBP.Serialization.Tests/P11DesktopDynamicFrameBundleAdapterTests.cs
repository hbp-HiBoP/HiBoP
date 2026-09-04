using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using HBP.RenderModelAdapters;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class P11DesktopDynamicFrameBundleAdapterTests
    {
        [Test]
        public void Expectations_FollowOrderedVisualizationMembershipAndTimelineInclusion()
        {
            ContractId visualizationId = Id(10);
            ContractId first = Id(20);
            ContractId excluded = Id(21);
            ContractId third = Id(22);
            ContractId cut = Id(30);
            SessionSnapshot snapshot = new(ContractVersion.V1, new SessionEpoch(Id(1), 1), new StateRevision(1), new[]
            {
                Column(first, true),
                Column(excluded, false),
                Column(third, true),
                new ScopeState(new ScopeKey(ScopeType.Visualization, Id(110)), new ScopeRevision(1), new[]
                {
                    new StateProperty(V1PropertyKeys.VisualizationEntity, ContractValue.FromId(visualizationId)),
                    new StateProperty(V1PropertyKeys.VisualizationColumnMembership, ContractValue.FromIds(new[] { first, excluded, third })),
                }),
            }, Array.Empty<AssetReference>());
            var content = new Dictionary<ContractId, DynamicColumnContent>
            {
                [first] = DynamicColumnContent.Surface,
                [third] = DynamicColumnContent.Surface | DynamicColumnContent.Sites,
            };
            var cuts = new Dictionary<ContractId, IReadOnlyList<ContractId>>
            {
                [third] = new[] { cut },
            };

            IReadOnlyList<DynamicColumnExpectation> result = DesktopDynamicFrameBundleAdapter.CaptureExpectations(snapshot, visualizationId, content, cuts);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].ColumnId, Is.EqualTo(first));
            Assert.That(result[1].ColumnId, Is.EqualTo(third));
            Assert.That(result[1].Content, Is.EqualTo(DynamicColumnContent.Surface | DynamicColumnContent.Sites));
            Assert.That(result[1].CutIds, Is.EqualTo(new[] { cut }));
        }

        private static ScopeState Column(ContractId columnId, bool included)
        {
            return new ScopeState(new ScopeKey(ScopeType.Column, Id(columnId.High + 100)), new ScopeRevision(1), new[]
            {
                new StateProperty(V1PropertyKeys.ColumnEntity, ContractValue.FromId(columnId)),
                new StateProperty(V1PropertyKeys.ColumnIncludedInTimeline, ContractValue.FromBoolean(included)),
            });
        }

        private static ContractId Id(ulong value) => new(value, value + 1);
    }
}
