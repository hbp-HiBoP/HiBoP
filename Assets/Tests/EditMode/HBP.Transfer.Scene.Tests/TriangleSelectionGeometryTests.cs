using System.Collections.Generic;
using System.IO;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer
{
    public sealed class TriangleSelectionGeometryTests
    {
        [Test]
        public void RaycastUsesOriginalFaceIdsThroughHiddenAndClippedFaces()
        {
            var geometry = new TriangleSelectionGeometry(new[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 0, 1),
                new Vector3(0, 0, 3), new Vector3(0, 1, 3), new Vector3(1, 0, 3)
            }, new[] { 0, 1, 2, 3, 4, 5 });
            var origin = new Vector3(.25f, .25f, 0);
            var direction = new Vector3(0, 0, 2);
            float distance = 10;
            Assert.That(geometry.Raycast(origin, direction, new[] { 0, 1 }, null, false, ref distance, out int face, out Vector3 weights), Is.True);
            Assert.That(face, Is.EqualTo(1));
            Assert.That(distance, Is.EqualTo(1.5f));
            Assert.That(weights, Is.EqualTo(new Vector3(.5f, .25f, .25f)));

            using var cut = new Cut(new Vector3(0, 0, 2), Vector3.back);
            var cuts = new List<Cut> { cut };
            distance = 10;
            Assert.That(geometry.Raycast(origin, direction, new[] { 1, 1 }, cuts, true, ref distance, out face, out _), Is.True);
            Assert.That(face, Is.EqualTo(1));
            distance = 10;
            Assert.That(geometry.Raycast(origin, direction, new[] { 0, 0 }, cuts, true, ref distance, out _, out _), Is.False);
        }

        [TestCase("1 0 1")]
        [TestCase("1 0 1\r\n0 1")]
        public void TrimaskReadsOnlyOriginalTopology(string file)
        {
            using var reader = new StringReader(file);
            Assert.That(TriangleEraser.ReadTrimask(reader), Is.EqualTo(new[] { 1, 0, 1 }));
        }

        [Test]
        public void AdjacencyRequiresASharedEdge()
        {
            var geometry = new TriangleSelectionGeometry(new Vector3[6], new[] { 0, 1, 2, 1, 0, 3, 2, 4, 5 });
            Assert.That(geometry.Neighbors(0), Is.EqualTo(new[] { 1 }));
            Assert.That(geometry.Neighbors(2), Is.Empty);
        }
    }
}
