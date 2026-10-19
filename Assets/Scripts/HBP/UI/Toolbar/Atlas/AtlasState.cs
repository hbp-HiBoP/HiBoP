using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Object3D;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Quest.Desktop;
using HBP.UI.Tools;

namespace HBP.UI.Toolbar
{
    public class AtlasState : Tool
    {
        #region Properties

        /// <summary>
        /// Displays the IBC atlas
        /// </summary>
        [SerializeField] private Toggle m_IBCToggle;

        /// <summary>
        /// Displays the JuBrain atlas
        /// </summary>
        [SerializeField] private Toggle m_JubrainToggle;

        /// <summary>
        /// Displays the MarsAtlas
        /// </summary>
        [SerializeField] private Toggle m_MarsAtlasToggle;

        [SerializeField] private Toggle m_DiFuMoToggle;

        [SerializeField] private Toggle m_LocalizersToggle;
        private bool m_AtlasRecoveryPending;

        #endregion

        #region Public Methods

        /// <summary>
        /// Add the listener to this tool
        /// </summary>
        public override void Initialize()
        {
            BindToggle(m_IBCToggle, () => "ibc", value => SelectedScene.FMRIManager.DisplayIBCContrasts = value);
            BindToggle(m_JubrainToggle, () => "jubrain", value => SelectedScene.AtlasManager.DisplayJuBrainAtlas = value);
            BindToggle(m_MarsAtlasToggle, () => "mars", value => SelectedScene.AtlasManager.DisplayMarsAtlas = value);
            BindToggle(m_DiFuMoToggle, () => "difumo:" + SelectedScene.FMRIManager.SelectedDiFuMoAtlas, value => SelectedScene.FMRIManager.DisplayDiFuMo = value);
            BindToggle(m_LocalizersToggle, () => "localizer:" + SelectedScene.FMRIManager.SelectedLocalizersProtocol, value => SelectedScene.FMRIManager.DisplayLocalizers = value);
        }

        private void BindToggle(Toggle toggle, Func<string> atlasId, Action<bool> apply)
        {
            toggle.onValueChanged.AddListener(async visible =>
            {
                if (ListenerLock) return;
                var scene = SelectedScene;
                if (visible && !await EnsureAtlasReadyAsync(scene, atlasId(), toggle)) return;
                if (scene != null && !scene.IsClosing && SelectedScene == scene)
                {
                    apply(visible);
                    // Read back the accepted domain state (the selected mesh may have changed during recovery).
                    bool wasLocked = ListenerLock;
                    ListenerLock = true;
                    try
                    {
                        UpdateStatus();
                    }
                    finally
                    {
                        ListenerLock = wasLocked;
                    }
                }
            });
        }

        private async UniTask<bool> EnsureAtlasReadyAsync(Base3DScene scene, string id, Toggle toggle)
        {
            try
            {
                scene.EnsureAtlasCanBeUsed(id);
                return true;
            }
            catch (InvalidOperationException)
            {
                // Rejected domain changes are expected UI failures, not bug-report exceptions.
                toggle.SetIsOnWithoutNotify(false);
            }

            if (m_AtlasRecoveryPending) return false;
            m_AtlasRecoveryPending = true;
            var lifetime = this.GetCancellationTokenOnDestroy();
            string message = $"The atlas '{id}' is not ready on both Desktop and Quest. It must be loaded and confirmed on both devices before it can be displayed.\n\nWake Quest, keep HiBoP Quest open, then choose Retry on Quest to try loading and confirming it again.";
            try
            {
                int answer = await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Atlas not ready on both devices", message, lifetime, "Retry on Quest", "Cancel");
                if (answer != 0 || scene == null || scene.IsClosing) return false;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                try
                {
                    if (!QuestManager.IsInitialized) throw new InvalidOperationException("Quest is not connected.");
                    await LoadingManager.PresentAsync(async (progress, token) =>
                    {
                        progress(0, 0, new LoadingText("Reloading atlas on Quest: " + id));
                        await QuestManager.Instance.EnsureAtlasReadyAsync(id, token);
                        progress(1, 0, new LoadingText("Atlas ready on both devices"));
                    }, timeout.Token);
                    if (scene == null || scene.IsClosing || SelectedScene != scene) return false;
                    scene.EnsureAtlasCanBeUsed(id);
                    Module3DMain.OnRequestUpdateInToolbar.Invoke();
                    return true;
                }
                catch (Exception) when (!lifetime.IsCancellationRequested)
                {
                    await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Atlas reload did not complete", $"The atlas '{id}' could not be confirmed on both Desktop and Quest.\n\nWake Quest and check its connection in the Quest window, then try again. The atlas display has remained disabled.", lifetime, "OK");
                    return false;
                }
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                m_AtlasRecoveryPending = false;
            }
        }

        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_IBCToggle.isOn = false;
            m_IBCToggle.interactable = false;
            m_JubrainToggle.isOn = false;
            m_JubrainToggle.interactable = false;
            m_MarsAtlasToggle.isOn = false;
            m_MarsAtlasToggle.interactable = false;
            m_DiFuMoToggle.isOn = false;
            m_DiFuMoToggle.interactable = false;
            m_LocalizersToggle.isOn = false;
            m_LocalizersToggle.interactable = false;
        }

        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool supportsMNIResources = SelectedScene.MeshManager.SelectedMesh.SupportsMNIResources;
            bool isIBCAvailable = Object3DManager.IBC.Loaded && supportsMNIResources;
            bool isJuBrainAtlasAvailable = Object3DManager.JuBrain.Loaded && supportsMNIResources;
            bool canUseMarsAtlas = Object3DManager.MarsAtlas.Loaded && SelectedScene.MeshManager.SelectedMesh.SupportsMarsAtlas;
            bool isDiFuMoAvailable = Object3DManager.DiFuMo.Loaded && supportsMNIResources;
            bool isLocalizersAvailable = Object3DManager.Localizers.Loaded && supportsMNIResources;

            m_IBCToggle.interactable = isIBCAvailable;
            m_JubrainToggle.interactable = isJuBrainAtlasAvailable;
            m_MarsAtlasToggle.interactable = canUseMarsAtlas;
            m_DiFuMoToggle.interactable = isDiFuMoAvailable;
            m_LocalizersToggle.interactable = isLocalizersAvailable;
        }

        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_IBCToggle.isOn = SelectedScene.FMRIManager.DisplayIBCContrasts;
            m_JubrainToggle.isOn = SelectedScene.AtlasManager.DisplayJuBrainAtlas;
            m_MarsAtlasToggle.isOn = SelectedScene.AtlasManager.DisplayMarsAtlas;
            m_DiFuMoToggle.isOn = SelectedScene.FMRIManager.DisplayDiFuMo;
            m_LocalizersToggle.isOn = SelectedScene.FMRIManager.DisplayLocalizers;
        }

        #endregion
    }
}
