using System;
using System.Linq;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed partial class QuestPointerInput
    {
        private const float SiteRepeatSeconds = .2f;
        private const float AtlasFeedbackSeconds = 2f;

        private enum AtlasSource
        {
            None,
            IBC,
            DiFuMo
        }

        private Base3DScene thumbstickScene;
        private Column3D thumbstickColumn;
        private AtlasSource thumbstickAtlas;
        private string atlasFeedback;
        private float atlasFeedbackUntil;

        private void UpdateThumbsticks()
        {
            var scene = anatomy != null ? anatomy.Scene : null;
            if (scene == null || !CanUseThumbsticks() || !anatomy.CanChangeDisplaySettings(scene))
            {
                CancelThumbsticks();
                return;
            }

            if (scene != thumbstickScene)
            {
                CancelThumbsticks();
                thumbstickScene = scene;
            }

            var column = scene.SelectedColumn;
            if (column != thumbstickColumn)
            {
                thumbstickColumn = column;
                hands[1].ThumbstickGesture = default;
            }

            var fmri = scene.FMRIManager;
            var source = fmri == null || !scene.MeshManager.SelectedMesh.SupportsMNIResources ? AtlasSource.None : fmri.DisplayIBCContrasts ? AtlasSource.IBC : fmri.DisplayDiFuMo ? AtlasSource.DiFuMo : AtlasSource.None;
            if (source != thumbstickAtlas)
            {
                thumbstickAtlas = source;
                hands[0].ThumbstickGesture = default;
                atlasFeedback = null;
            }

            float now = Time.unscaledTime;
            if (!hands[0].Valid || now >= atlasFeedbackUntil) atlasFeedback = null;

            var siteStep = SampleThumbstick(hands[1], column != null && column.SelectedSite != null && IsThumbstickColumnPresented(column), now, SiteRepeatSeconds);
            if (siteStep.x != 0)
                scene.NavigateSelectedSite(siteStep.x < 0 ? SiteNavigationDirection.Left : SiteNavigationDirection.Right);

            var atlasStep = SampleThumbstick(hands[0], source != AtlasSource.None, now);
            if (atlasStep == Vector2Int.zero) return;
            var message = StepAtlas(scene, source, atlasStep);
            if (message == null) return;
            ClearAtlasHover();
            atlasFeedback = message;
            atlasFeedbackUntil = now + AtlasFeedbackSeconds;
        }

        private bool CanUseThumbsticks()
        {
            if (modalDialog != null || LoadingManager.IsPresenting || QuestDropdown.IsAnyPopupOpen || TouchScreenKeyboard.visible || siteProbe != null && siteProbe.IsActive) return false;
            foreach (var hand in hands)
                if (hand.Capture.Owner is QuestInteractionOwner.UI or QuestInteractionOwner.Anatomy or QuestInteractionOwner.Cut or QuestInteractionOwner.ROI)
                    return false;
            var selected = eventSystem.currentSelectedGameObject;
            return selected == null || !selected.TryGetComponent<InputField>(out var field) || !(field is QuestTextInputField questField ? questField.IsEditing : field.isFocused);
        }

        private bool IsThumbstickColumnPresented(Column3D selected)
        {
            foreach (var column in anatomy.Columns)
                if (column.Column == selected)
                    return anatomy.IsColumnPresented(column);
            return false;
        }

        private static Vector2Int SampleThumbstick(Hand hand, bool available, float now, float repeat = 0)
        {
            var action = hand.Thumbstick;
            bool valid = available && hand.Valid && action.controls.Count > 0;
            return hand.ThumbstickGesture.Sample(valid ? action.ReadValue<Vector2>() : Vector2.zero, valid, now, repeat);
        }

        private void CancelThumbsticks()
        {
            thumbstickScene = null;
            thumbstickColumn = null;
            thumbstickAtlas = AtlasSource.None;
            atlasFeedback = null;
            foreach (var hand in hands) hand.ThumbstickGesture = default;
        }

        private static string StepAtlas(Base3DScene scene, AtlasSource source, Vector2Int step)
        {
            var fmri = scene.FMRIManager;
            if (source == AtlasSource.IBC)
            {
                if (step.x == 0 || !CanStepAtlas(scene, "ibc")) return null;
                var labels = Object3DManager.IBC.Information.AllLabels;
                int next = fmri.SelectedIBCContrastID + step.x;
                if (next < 0 || next >= labels.Count || next >= fmri.CurrentFMRI.Volumes.Count) return null;
                fmri.SelectedIBCContrastID = next;
                return $"IBC\n{labels[next].PrettyName}";
            }

            var difumo = Object3DManager.DiFuMo;
            string version = fmri.SelectedDiFuMoAtlas;
            if (!CanStepAtlas(scene, "difumo:" + version)) return null;
            if (step.y != 0)
            {
                int dimension = int.Parse(version);
                foreach (var candidate in difumo.FMRIs.Keys.OrderBy(value => step.y * int.Parse(value)))
                {
                    if (step.y * (int.Parse(candidate) - dimension) <= 0 || !CanStepAtlas(scene, "difumo:" + candidate)) continue;
                    var labels = difumo.Information[candidate].AllLabels;
                    if (labels.Count == 0 || difumo.FMRIs[candidate].Volumes.Count == 0) continue;
                    fmri.SelectedDiFuMoAtlas = candidate;
                    return $"DiFuMo {candidate}\n{labels[0].Name}";
                }

                return null;
            }

            var components = difumo.Information[version].AllLabels;
            int area = fmri.SelectedDiFuMoArea + step.x;
            if (area < 0 || area >= components.Count || area >= fmri.CurrentFMRI.Volumes.Count) return null;
            fmri.SelectedDiFuMoArea = area;
            return $"DiFuMo {version}\n{components[area].Name}";
        }

        private static bool CanStepAtlas(Base3DScene scene, string atlasId)
        {
            if (!AtlasResources.IsLoaded(atlasId)) return false;
            try
            {
                scene.EnsureAtlasCanBeUsed(atlasId);
                return true;
            }
            catch (InvalidOperationException)
            {
                // A shared atlas may still be preparing on the other peer, or reserved for unload.
                return false;
            }
        }
    }
}
