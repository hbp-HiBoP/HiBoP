using System;
using System.Collections.Generic;
using System.Reflection;
using HBP.Core.Data;
using HBP.Data.Informations;
using HBP.UI.Informations;
using NUnit.Framework;
using UnityEngine;
using Grid = HBP.Data.Informations.TrialMatrix.TrialMatrixGrid;
using MatrixData = HBP.Data.Informations.TrialMatrix.Data;

namespace HBP.Tests.ProjectWorkflow
{
    public sealed class InformationPresentationTests
    {
        private static readonly Type Model = typeof(InformationsWrapper).Assembly.GetType("HBP.UI.Informations.InformationPresentation", true);
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static object Create() => Activator.CreateInstance(Model, Instance, null, new object[] { null, null }, null);
        private static object Property(object model, string name) => Model.GetProperty(name, Instance).GetValue(model);
        private static object Call(object model, string method, params object[] args) => Model.GetMethod(method, Instance).Invoke(model, args);

        [Test]
        public void GraphOnlyRebuildPreservesTheMatrixModelAndSampleRevision()
        {
            var model = Create();
            try
            {
                Call(model, "Rebuild", Array.Empty<ChannelStruct>(), Array.Empty<HBP.Data.Informations.Column>(), true);
                var matrix = Property(model, "Matrices");
                var samples = Property(model, "SamplesRevision");
                Call(model, "Rebuild", Array.Empty<ChannelStruct>(), Array.Empty<HBP.Data.Informations.Column>(), false);
                Assert.That(Property(model, "Matrices"), Is.SameAs(matrix));
                Assert.That(Property(model, "SamplesRevision"), Is.EqualTo(samples));
                Assert.That((long)Property(model, "Revision"), Is.GreaterThan(0));
            }
            finally
            {
                ((IDisposable)model).Dispose();
            }
        }

        [Test]
        public void ReturningToAutomaticLimitsUsesTheNextSitesOwnDefaults()
        {
            var model = Create();
            var data = Data();
            data.Limits = new Vector2(-1, 1);
            try
            {
                Call(model, "SetMatrixLimits", data, new Vector2(-10, 10));
                Assert.That(Call(model, "GetMatrixLimits", data), Is.EqualTo(new Vector2(-10, 10)));
                Call(model, "SetMatrixLimits", data, data.Limits);
                var next = new MatrixData(data.DataStruct, Array.Empty<ChannelStruct>()) { Limits = new Vector2(20, 30) };
                Assert.That(Call(model, "GetMatrixLimits", next), Is.EqualTo(new Vector2(20, 30)));
            }
            finally
            {
                ((IDisposable)model).Dispose();
            }
        }

        [Test]
        public void CcepSourcesHaveIndependentManualLimits()
        {
            var model = Create();
            var ordinary = Data();
            var patient = new Patient("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), "", "patient");
            var a = new MatrixData(new Grid.CCEPTrialMatrixData(ordinary.DataStruct.Dataset, "responses", new List<Bloc>(), new ChannelStruct("A1", patient)), Array.Empty<ChannelStruct>()) { Limits = new Vector2(-1, 1) };
            var b = new MatrixData(new Grid.CCEPTrialMatrixData(ordinary.DataStruct.Dataset, "responses", new List<Bloc>(), new ChannelStruct("A2", patient)), Array.Empty<ChannelStruct>()) { Limits = new Vector2(-2, 2) };
            try
            {
                Call(model, "SetMatrixLimits", a, new Vector2(-50, 50));
                Assert.That(Call(model, "GetMatrixLimits", b), Is.EqualTo(b.Limits));
            }
            finally
            {
                ((IDisposable)model).Dispose();
            }
        }

        private static MatrixData Data()
        {
            var protocol = new Protocol("protocol", Array.Empty<Bloc>(), "protocol");
            var dataset = new Dataset("recording", protocol, Array.Empty<DataInfo>(), "dataset");
            return new MatrixData(new Grid.IEEGTrialMatrixData(dataset, "signal", new List<Bloc>()), Array.Empty<ChannelStruct>());
        }
    }
}
