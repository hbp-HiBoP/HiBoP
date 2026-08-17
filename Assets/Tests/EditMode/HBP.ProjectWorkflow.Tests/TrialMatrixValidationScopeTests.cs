using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.UI.Database;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HBP.Tests.ProjectWorkflow
{
    public class TrialMatrixValidationScopeTests
    {
        [Test]
        public void SelectDataInfos_KeepsOnlySelectedPatientsAndDataName()
        {
            Patient selectedPatient = CreatePatient("selected");
            Patient otherPatient = CreatePatient("other");
            Protocol protocol = new("protocol", Array.Empty<Bloc>(), "protocol");
            IEEGDataInfo selected = CreateDataInfo("recording", selectedPatient, protocol, "selected-data");
            IEEGDataInfo otherPatientData = CreateDataInfo("recording", otherPatient, protocol, "other-data");
            IEEGDataInfo otherName = CreateDataInfo("other-recording", selectedPatient, protocol, "other-name");
            MethodInfo selectDataInfos = typeof(TrialMatrixDisplayer).GetMethod("SelectDataInfos", BindingFlags.Static | BindingFlags.NonPublic);

            List<IEEGDataInfo> result = (List<IEEGDataInfo>)selectDataInfos.Invoke(null, new object[]
            {
                new[] { selected, otherPatientData, otherName },
                new[] { selectedPatient },
                "recording"
            });

            Assert.That(result, Is.EqualTo(new[] { selected }));
        }

        private static Patient CreatePatient(string id)
        {
            return new Patient(id, Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, id);
        }

        private static IEEGDataInfo CreateDataInfo(string name, Patient patient, Protocol protocol, string id)
        {
            return new IEEGDataInfo(name, protocol, new EDF($"{id}.edf", Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.Auto, string.Empty, id);
        }
    }
}
