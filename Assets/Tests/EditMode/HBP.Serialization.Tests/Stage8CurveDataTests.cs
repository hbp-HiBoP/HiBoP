using System;
using HBP.Data.Informations.Graphs;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class Stage8CurveDataTests
    {
        [Test]
        public void RegularCurve_ExposesExactImplicitAbscissaeWithoutMaterializingPoints()
        {
            float[] values = { 2, 4, 8, 16 };
            CurveData curve = CurveData.CreateRegular(values, -3, 9, Color.red);

            Assert.That(curve.IsRegular, Is.True);
            Assert.That(curve.Count, Is.EqualTo(4));
            Assert.That(curve.HasMaterializedPoints, Is.False);
            Assert.That(curve.GetPoint(0), Is.EqualTo(new Vector2(-3, 2)));
            Assert.That(curve.GetPoint(1), Is.EqualTo(new Vector2(1, 4)));
            Assert.That(curve.GetPoint(3), Is.EqualTo(new Vector2(9, 16)));
            Assert.That(curve.HasMaterializedPoints, Is.False);
        }

        [Test]
        public void RegularCurve_RetainsCanonicalValueArrayByReference()
        {
            float[] values = { 1, 2, 3 };
            CurveData curve = CurveData.CreateRegular(values, 0, 2, Color.white);

            values[1] = 42;

            Assert.That(curve.GetOrdinate(1), Is.EqualTo(42));
            Assert.That(curve.HasMaterializedPoints, Is.False);
        }

        [Test]
        public void ExplicitCurve_DoesNotCopyAnExistingPointArray()
        {
            Vector2[] points = { new(1, 2), new(3, 4) };
            CurveData curve = CurveData.CreateInstance(points, Color.green);

            Assert.That(curve.Points, Is.SameAs(points));
            Assert.That(curve.IsRegular, Is.False);
        }

        [Test]
        public void PointsCompatibilityAccessor_MaterializesRegularCurveExactlyOnce()
        {
            CurveData curve = CurveData.CreateRegular(new[] { 5f, 6f, 7f }, 10, 20, Color.blue);

            Vector2[] first = curve.Points;
            Vector2[] second = curve.Points;

            Assert.That(second, Is.SameAs(first));
            Assert.That(first, Is.EqualTo(new[] { new Vector2(10, 5), new Vector2(15, 6), new Vector2(20, 7) }));
        }

        [Test]
        public void SingleSampleCurve_UsesStartAsItsOnlyAbscissa()
        {
            CurveData curve = CurveData.CreateRegular(new[] { 12f }, 7, 99, Color.white);

            Assert.That(curve.GetPoint(0), Is.EqualTo(new Vector2(7, 12)));
        }

        [Test]
        public void ShapedRegularCurve_RetainsValuesAndShapesWithoutPointMaterialization()
        {
            float[] values = { 1, 3, 5 };
            float[] shapes = { 0.1f, 0.2f, 0.3f };
            ShapedCurveData curve = ScriptableObject.CreateInstance<ShapedCurveData>();
            curve.InitRegular(values, shapes, -1, 1, Color.cyan, 3);

            Assert.That(curve.IsRegular, Is.True);
            Assert.That(curve.Shapes, Is.SameAs(shapes));
            Assert.That(curve.GetPoint(2), Is.EqualTo(new Vector2(1, 5)));
            Assert.That(curve.HasMaterializedPoints, Is.False);
        }

        [Test]
        public void InvalidPointIndex_IsRejected()
        {
            CurveData curve = CurveData.CreateRegular(new[] { 1f }, 0, 0, Color.white);

            Assert.Throws<ArgumentOutOfRangeException>(() => curve.GetPoint(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => curve.GetOrdinate(-1));
        }

        [Test]
        public void RepeatedIndexedReads_DoNotAllocate()
        {
            CurveData curve = CurveData.CreateRegular(new[] { 1f, 2f, 3f, 4f }, 0, 3, Color.white);
            Vector2 accumulator = curve.GetPoint(0);
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 10_000; i++)
                accumulator += curve.GetPoint(i & 3);

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0));
            Assert.That(accumulator.x, Is.GreaterThan(0));
        }
    }
}
