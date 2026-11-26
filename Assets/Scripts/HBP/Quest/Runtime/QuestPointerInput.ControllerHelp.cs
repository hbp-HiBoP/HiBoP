using UnityEngine;

namespace HBP.Quest
{
    public sealed partial class QuestPointerInput
    {
        private bool UpdateControllerHelp()
        {
            if (controllerCalloutPrefab == null) return false;
            controllerHelp ??= new QuestControllerHelp(controllerCalloutPrefab, transform, left, right);

            float now = Time.unscaledTime;
            bool blocked = !focused || paused || head == null || !head.IsTracked || !CanUseThumbsticks();
            foreach (var hand in hands)
            {
                if (!hand.Valid) continue;
                if (hand.ControllerHelpNearTarget || hand.ControllerHelpRayTarget != null && now - hand.ControllerHelpAimSince >= .2f || hand.Primary.IsPressed() || hand.Secondary.IsPressed() || hand.Thumbstick.ReadValue<Vector2>().sqrMagnitude > .35f * .35f)
                    blocked = true;
            }

            float deltaTime = Time.unscaledDeltaTime;
            Transform headPose = head != null ? head.transform : null;
            bool showLeft = controllerHelp.Sample(0, headPose, blocked || !hands[0].Valid, deltaTime);
            bool showRight = controllerHelp.Sample(1, headPose, blocked || !hands[1].Valid, deltaTime);
            if (!showLeft && !showRight) return false;

            bool brain = false;
            bool sites = false;
            if (anatomy != null)
                foreach (var column in anatomy.AllColumns)
                {
                    if (!anatomy.CanInteractWithColumn(column)) continue;
                    brain = true;
                    if (column.Column?.Sites?.Count > 0)
                    {
                        sites = true;
                        break;
                    }
                }

            bool crossed = showLeft && showRight && Vector3.Dot(head.transform.right, right.transform.position - left.transform.position) < 0;
            bool erasing = triangles != null && triangles.CapturesGrip;
            if (showLeft)
                controllerHelp.Show(0, headPose, crossed ? 1 : -1, brain, sites, erasing, thumbstickAtlas == AtlasSource.IBC, thumbstickAtlas == AtlasSource.DiFuMo, deltaTime);
            if (showRight)
                controllerHelp.Show(1, headPose, crossed ? -1 : 1, brain, sites, erasing, false, false, deltaTime);
            return true;
        }

        [BeforeRenderOrder(100)]
        private void RefreshControllerHelpBeforeRender()
        {
            // The trackers apply the latest grip pose earlier in Application.onBeforeRender.
            if (head != null && head.IsTracked) controllerHelp?.RefreshLeaderStarts();
            else controllerHelp?.Hide();
        }
    }
}
