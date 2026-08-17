using System.Linq;
using HBP.Core.Data;
using NUnit.Framework;

namespace HBP.Tests.Serialization.Helpers
{
    internal static class ProjectSnapshotAssert
    {
        public static void AreFunctionallyEquivalent(Project expected, Project actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Preferences.ID, Is.EqualTo(expected.Preferences.ID));
            Assert.That(actual.Patients.Select(p => p.ID), Is.EquivalentTo(expected.Patients.Select(p => p.ID)));
            Assert.That(actual.Groups.Select(g => g.ID), Is.EquivalentTo(expected.Groups.Select(g => g.ID)));
            Assert.That(actual.Datasets.Select(d => d.ID), Is.EquivalentTo(expected.Datasets.Select(d => d.ID)));
            Assert.That(actual.Visualizations.Select(v => v.ID), Is.EquivalentTo(expected.Visualizations.Select(v => v.ID)));

            if (expected.Patients.Count > 0)
            {
                Patient expectedPatient = expected.Patients[0];
                Patient actualPatient = actual.Patients.First(p => p.ID == expectedPatient.ID);
                Assert.That(actualPatient.Name, Is.EqualTo(expectedPatient.Name));
                Assert.That(actualPatient.Sites.Select(s => s.ID), Is.EquivalentTo(expectedPatient.Sites.Select(s => s.ID)));
                Assert.That(actualPatient.Tags.Select(t => t.ID), Is.EquivalentTo(expectedPatient.Tags.Select(t => t.ID)));
            }

            if (expected.Datasets.Count > 0)
            {
                Dataset expectedDataset = expected.Datasets[0];
                Dataset actualDataset = actual.Datasets.First(d => d.ID == expectedDataset.ID);
                Assert.That(actualDataset.Data.Select(d => d.ID), Is.EquivalentTo(expectedDataset.Data.Select(d => d.ID)));
                Assert.That(actualDataset.Data.Select(d => d.DataContainer.ID), Is.EquivalentTo(expectedDataset.Data.Select(d => d.DataContainer.ID)));
            }

            if (expected.Visualizations.Count > 0)
            {
                Visualization expectedVisualization = expected.Visualizations[0];
                Visualization actualVisualization = actual.Visualizations.First(v => v.ID == expectedVisualization.ID);
                Assert.That(actualVisualization.Columns.Select(c => c.ID), Is.EquivalentTo(expectedVisualization.Columns.Select(c => c.ID)));
                Assert.That(actualVisualization.Columns.Select(c => c.BaseConfiguration.ID), Is.EquivalentTo(expectedVisualization.Columns.Select(c => c.BaseConfiguration.ID)));
            }
        }
    }
}
