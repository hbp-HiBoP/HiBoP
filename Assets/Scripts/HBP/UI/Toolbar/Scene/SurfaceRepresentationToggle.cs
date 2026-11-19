using System;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class SurfaceRepresentationToggle : Tool
    {
        [SerializeField] private Toggle m_Toggle;
        [SerializeField] private Image m_Icon;
        [SerializeField] private Tooltip m_Tooltip;

        private Base3DScene m_SubscribedScene;

        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener(isInflated =>
            {
                if (ListenerLock) return;
                ChangeRepresentationAsync(isInflated ? SurfaceRepresentation.Inflated : SurfaceRepresentation.Anatomical).Forget();
            });
        }

        public override void DefaultState()
        {
            ChangeSceneSubscription(null);
            m_Toggle.SetIsOnWithoutNotify(false);
            m_Toggle.interactable = false;
        }

        public override void UpdateInteractable()
        {
            ChangeSceneSubscription(SelectedScene);
            if (SelectedScene == null || SelectedScene.MeshManager.Meshes.Count == 0)
            {
                m_Toggle.interactable = false;
                return;
            }

            SurfaceRepresentation current = SelectedScene.MeshManager.SelectedMesh.Representation;
            bool available = SelectedScene.MeshManager.SelectedMesh.TryGetInflationAvailability(out _);
            m_Toggle.interactable = !SelectedScene.IsSurfaceRepresentationTransitioning && !SelectedScene.IsSurfaceRepresentationPreparing && (current == SurfaceRepresentation.Inflated || available);
        }

        public override void UpdateStatus()
        {
            ChangeSceneSubscription(SelectedScene);
            bool isInflated = SelectedScene.MeshManager.SelectedMesh.Representation == SurfaceRepresentation.Inflated;
            m_Toggle.SetIsOnWithoutNotify(isInflated);
        }

        private async UniTaskVoid ChangeRepresentationAsync(SurfaceRepresentation representation)
        {
            Base3DScene scene = SelectedScene;
            if (scene == null) return;

            m_Toggle.interactable = false;
            try
            {
                await SurfaceInflationLoading.ChangeRepresentationAsync(scene, representation);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (this != null && scene != null && ReferenceEquals(scene, SelectedScene))
                {
                    m_Toggle.SetIsOnWithoutNotify(scene.MeshManager.SelectedMesh.Representation == SurfaceRepresentation.Inflated);
                    UpdateInteractable();
                }
            }
        }

        private void ChangeSceneSubscription(Base3DScene scene)
        {
            if (ReferenceEquals(scene, m_SubscribedScene)) return;
            if (m_SubscribedScene != null)
            {
                m_SubscribedScene.OnAddCut.RemoveListener(OnCutAdded);
                m_SubscribedScene.OnSurfaceRepresentationChanged.RemoveListener(OnSurfaceRepresentationChanged);
            }

            m_SubscribedScene = scene;
            if (m_SubscribedScene != null)
            {
                m_SubscribedScene.OnAddCut.AddListener(OnCutAdded);
                m_SubscribedScene.OnSurfaceRepresentationChanged.AddListener(OnSurfaceRepresentationChanged);
            }
        }

        private void OnCutAdded(Core.Object3D.Cut cut)
        {
            if (m_SubscribedScene != null && m_SubscribedScene.Cuts.Count == 1 && m_SubscribedScene.MeshManager.SelectedMesh.Representation == SurfaceRepresentation.Inflated)
            {
                SurfaceInflationLoading.ShowCutsInformationIfNeeded(m_SubscribedScene);
            }
        }

        private void OnSurfaceRepresentationChanged(SurfaceRepresentation representation)
        {
            if (representation == SurfaceRepresentation.Inflated && m_SubscribedScene != null && m_SubscribedScene.Cuts.Count > 0)
            {
                SurfaceInflationLoading.ShowCutsInformationIfNeeded(m_SubscribedScene);
            }
        }

        private void OnDestroy()
        {
            ChangeSceneSubscription(null);
        }
    }
}
