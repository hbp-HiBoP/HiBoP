using HBP.Quest;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Quest
{
    public sealed class QuestThumbstickGestureTests
    {
        [Test]
        public void AtlasDeflection_UsesOneDominantAxisAndRequiresNeutral()
        {
            var gesture = new QuestThumbstickGesture();
            Assert.That(gesture.Sample(Vector2.zero, true, 0), Is.EqualTo(Vector2Int.zero));
            Assert.That(gesture.Sample(new Vector2(.7f, .8f), true, 1), Is.EqualTo(Vector2Int.up));
            Assert.That(gesture.Sample(Vector2.right, true, 2), Is.EqualTo(Vector2Int.zero));
            Assert.That(gesture.Sample(Vector2.right * .5f, true, 3), Is.EqualTo(Vector2Int.zero));
            Assert.That(gesture.Sample(Vector2.right, true, 4), Is.EqualTo(Vector2Int.zero));
            gesture.Sample(Vector2.zero, true, 5);
            Assert.That(gesture.Sample(new Vector2(-.9f, -.7f), true, 6), Is.EqualTo(Vector2Int.left));
        }

        [Test]
        public void SiteDeflection_RepeatsOnlyWhileHeldInItsOriginalHorizontalDirection()
        {
            var gesture = new QuestThumbstickGesture();
            const float interval = .2f;
            gesture.Sample(Vector2.zero, true, 0, interval);
            Assert.That(gesture.Sample(Vector2.right, true, 0, interval), Is.EqualTo(Vector2Int.right));
            Assert.That(gesture.Sample(Vector2.right, true, .19f, interval), Is.EqualTo(Vector2Int.zero));
            Assert.That(gesture.Sample(Vector2.right, true, .2f, interval), Is.EqualTo(Vector2Int.right));
            Assert.That(gesture.Sample(Vector2.up, true, .4f, interval), Is.EqualTo(Vector2Int.zero));
            Assert.That(gesture.Sample(Vector2.right, true, .6f, interval), Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void BlockedGesture_DoesNotResumeUntilNeutralIsObservedWhileAvailable()
        {
            var gesture = new QuestThumbstickGesture();
            gesture.Sample(Vector2.zero, true, 0);
            Assert.That(gesture.Sample(Vector2.left, false, 1), Is.EqualTo(Vector2Int.zero));
            gesture.Sample(Vector2.zero, false, 2);
            Assert.That(gesture.Sample(Vector2.left, true, 3), Is.EqualTo(Vector2Int.zero));
            gesture.Sample(Vector2.zero, true, 4);
            Assert.That(gesture.Sample(Vector2.left, true, 5), Is.EqualTo(Vector2Int.left));
        }
    }
}
