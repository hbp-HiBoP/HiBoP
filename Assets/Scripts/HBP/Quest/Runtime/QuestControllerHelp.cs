using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>One hand's dwell and hysteresis for the controller legend.</summary>
    public struct QuestControllerHelpGesture
    {
        private const float DwellSeconds = 1.5f;
        private const float LookAwaySeconds = .18f;
        private float elapsed;

        public bool Visible { get; private set; }

        public bool Sample(bool blocked, bool inspecting, bool stillInView, float deltaTime)
        {
            if (blocked)
            {
                this = default;
                return false;
            }

            if (Visible)
            {
                elapsed = stillInView ? 0 : elapsed + deltaTime;
                if (elapsed >= LookAwaySeconds) this = default;
            }
            else
            {
                elapsed = inspecting ? elapsed + deltaTime : 0;
                if (elapsed >= DwellSeconds)
                {
                    Visible = true;
                    elapsed = 0;
                }
            }

            return Visible;
        }
    }

    /// <summary>Displays non-interactive, world-space callouts for the tracked Touch Plus controllers.</summary>
    internal sealed class QuestControllerHelp
    {
        private const float AngularScale = .00085f;
        private const float RowGap = 5;

        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Hand[] hands;

        private sealed class Hand
        {
            public readonly QuestDevicePoseTracker Tracker;
            public QuestControllerHelpGesture Gesture;
            public bool HasPose;
            public Vector3 Position;
            public Quaternion Rotation;
            public readonly Callout[] Callouts = new Callout[5];
            public readonly int[] RowOrder = new int[5];
            public int RowCount;

            public Hand(QuestDevicePoseTracker tracker) => Tracker = tracker;
        }

        private sealed class Callout
        {
            public readonly RectTransform Panel;
            public readonly Text Label;
            public readonly LineRenderer Leader;
            public QuestControllerControl Control;
            public float ScreenHeight;
            public float Height;

            public Callout(GameObject instance)
            {
                Panel = (RectTransform)instance.transform;
                Label = instance.GetComponentInChildren<Text>(true);
                Leader = instance.GetComponent<LineRenderer>();
                Leader.positionCount = 3;
            }
        }

        public QuestControllerHelp(GameObject prefab, Transform parent, QuestDevicePoseTracker left, QuestDevicePoseTracker right)
        {
            this.prefab = prefab;
            this.parent = parent;
            hands = new[] { new Hand(left), new Hand(right) };
        }

        public bool Sample(int index, Transform head, bool blocked, float deltaTime)
        {
            var hand = hands[index];
            blocked |= hand.Tracker == null || !hand.Tracker.HasControllerHelp;
            bool inspecting = false;
            bool stillInView = false;
            if (!blocked)
            {
                var controller = hand.Tracker.transform;
                Vector3 position = controller.position;
                Quaternion rotation = controller.rotation;
                Vector3 offset = position - head.position;
                float distance = offset.magnitude;
                Vector3 direction = offset.normalized;
                float headDown = Vector3.Dot(head.forward, Vector3.down);
                float look = Vector3.Dot(head.forward, direction);
                float face = Mathf.Abs(Vector3.Dot(controller.up, direction));
                bool steady = hand.HasPose && Vector3.Distance(position, hand.Position) <= .001f + .13f * deltaTime && Quaternion.Angle(rotation, hand.Rotation) <= .35f + 34 * deltaTime;

                inspecting = steady && headDown >= .14f && offset.y < -.08f && look >= .87f && face >= .48f && distance >= .22f && distance <= .66f;
                stillInView = headDown >= .07f && offset.y < -.045f && look >= .77f && distance >= .17f && distance <= .76f;
                hand.Position = position;
                hand.Rotation = rotation;
            }

            hand.HasPose = !blocked;
            bool visible = hand.Gesture.Sample(blocked, inspecting, stillInView, deltaTime);
            if (!visible) HideHand(hand);
            return visible;
        }

        public void Show(int index, Transform head, float side, bool brain, bool sites, bool erasing, bool ibc, bool difumo, float deltaTime)
        {
            var hand = hands[index];
            var controller = hand.Tracker.transform;
            bool left = index == 0;
            int count = 0;
            float height = 0;
            if (left)
            {
                Add("<b>Button X</b>\nHold to recenter.", QuestControllerControl.PrimaryButton);
                if (ibc || difumo)
                    Add(ibc ? "<b>Thumbstick</b>\nAdjust IBC contrast." : "<b>Thumbstick</b>\nChoose a component and dimension.", QuestControllerControl.Thumbstick);
            }
            else if (sites)
            {
                Add("<b>Button B</b>\nApply or undo a site preset.", QuestControllerControl.SecondaryButton);
                Add("<b>Button A</b>\nHold to select sites.", QuestControllerControl.PrimaryButton);
                Add("<b>Thumbstick</b>\nBrowse sites.", QuestControllerControl.Thumbstick);
            }

            if (brain)
                Add(erasing ? "<b>Side grip</b>\nSelect areas to erase." : "<b>Side grip</b>\nInteract with cuts and ROIs, or select areas to erase.", QuestControllerControl.SideGrip);

            Add(brain ? "<b>Front trigger</b>\nInteract with windows and brains." : "<b>Front trigger</b>\nInteract with menus.", QuestControllerControl.FrontTrigger);

            if (count == 0)
            {
                hand.RowCount = 0;
                HideFrom(hand, 0);
                hand.Tracker.SetControllerHelpVisible(true);
                return;
            }

            Vector3 offset = controller.position - head.position;
            float depth = Mathf.Max(.33f, Vector3.Dot(offset, head.forward));
            float scale = AngularScale * depth;
            float width = hand.Callouts[0].Panel.rect.width * scale;
            float x = Vector3.Dot(offset, head.right);
            float edge = side > 0 ? Mathf.Min(x + .12f * depth, .72f * depth - width) : Mathf.Max(x - .12f * depth, -.72f * depth + width);
            float halfHeight = (height - RowGap) * scale * .5f;
            float maxCenter = Mathf.Max(0, .46f * depth - halfHeight);
            float y = Mathf.Clamp(Vector3.Dot(offset, head.up), -maxCenter, maxCenter) + halfHeight;
            float blend = 1 - Mathf.Exp(-22 * deltaTime);
            bool newLayout = hand.RowCount != count || !hand.Callouts[0].Panel.gameObject.activeSelf;
            hand.RowCount = count;

            for (int i = 0; i < count; i++)
            {
                var callout = hand.Callouts[i];
                Vector3 anchor = hand.Tracker.ControllerHelpAnchor(callout.Control);
                callout.Leader.SetPosition(0, anchor);
                Vector3 fromHead = anchor - head.position;
                callout.ScreenHeight = Vector3.Dot(fromHead, head.up) / Mathf.Max(.1f, Vector3.Dot(fromHead, head.forward));
                if (newLayout) hand.RowOrder[i] = i;
            }

            // Keep the leaders in screen order, with a small dead zone against wrist jitter.
            float sortTolerance = newLayout ? 0 : .012f;
            for (int i = 1; i < count; i++)
            {
                int next = hand.RowOrder[i];
                int row = i;
                while (row > 0 && hand.Callouts[next].ScreenHeight > hand.Callouts[hand.RowOrder[row - 1]].ScreenHeight + sortTolerance)
                {
                    hand.RowOrder[row] = hand.RowOrder[row - 1];
                    row--;
                }

                hand.RowOrder[row] = next;
            }

            for (int i = 0; i < count; i++)
            {
                var callout = hand.Callouts[hand.RowOrder[i]];
                float rowHalfHeight = callout.Height * scale * .5f;
                Vector3 position = head.position + head.forward * depth + head.right * edge + head.up * (y - rowHalfHeight);
                var panel = callout.Panel;
                panel.pivot = new Vector2(side > 0 ? 0 : 1, .5f);
                panel.localScale = Vector3.one * scale;
                bool wasVisible = panel.gameObject.activeSelf;
                panel.SetPositionAndRotation(wasVisible ? Vector3.Lerp(panel.position, position, blend) : position, head.rotation);
                Vector3 end = panel.position;
                callout.Leader.widthMultiplier = .0022f * depth;
                callout.Leader.SetPosition(1, end - side * head.right * (.035f * depth));
                callout.Leader.SetPosition(2, end);
                if (!wasVisible) panel.gameObject.SetActive(true);
                y -= 2 * rowHalfHeight + RowGap * scale;
            }

            HideFrom(hand, count);
            hand.Tracker.SetControllerHelpVisible(true);

            void Add(string text, QuestControllerControl control)
            {
                if (!hand.Tracker.HasControllerHelpAnchor(control)) return;
                var callout = hand.Callouts[count];
                if (callout == null)
                {
                    var instance = Object.Instantiate(prefab, parent);
                    instance.SetActive(false);
                    hand.Callouts[count] = callout = new Callout(instance);
                }

                if (callout.Height == 0 || callout.Label.text != text)
                {
                    callout.Label.text = text;
                    callout.Height = callout.Label.preferredHeight + 10;
                    callout.Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, callout.Height);
                }

                callout.Control = control;
                count++;
                height += callout.Height + RowGap;
            }
        }

        public void RefreshLeaderStarts()
        {
            foreach (var hand in hands)
            {
                if (!hand.Gesture.Visible) continue;
                if (hand.Tracker == null || !hand.Tracker.IsTracked)
                {
                    hand.HasPose = false;
                    hand.Gesture = default;
                    HideHand(hand);
                    continue;
                }

                for (int i = 0; i < hand.RowCount; i++)
                {
                    var callout = hand.Callouts[i];
                    if (hand.Tracker.HasControllerHelpAnchor(callout.Control))
                        callout.Leader.SetPosition(0, hand.Tracker.ControllerHelpAnchor(callout.Control));
                }
            }
        }

        public void Hide()
        {
            foreach (var hand in hands)
            {
                hand.HasPose = false;
                hand.Gesture = default;
                HideHand(hand);
            }
        }

        private static void HideHand(Hand hand)
        {
            if (hand.Tracker != null) hand.Tracker.SetControllerHelpVisible(false);
            HideFrom(hand, 0);
        }

        private static void HideFrom(Hand hand, int count)
        {
            for (int i = count; i < hand.Callouts.Length; i++)
                if (hand.Callouts[i]?.Panel.gameObject.activeSelf == true)
                    hand.Callouts[i].Panel.gameObject.SetActive(false);
        }
    }
}
