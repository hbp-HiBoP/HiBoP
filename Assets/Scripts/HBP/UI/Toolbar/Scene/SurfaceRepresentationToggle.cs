using System;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Exceptions;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class SurfaceRepresentationToggle : Tool
    {
        private const string CUTS_INFORMATION = "In inflated mode, cuts remain defined in anatomical coordinates. They continue to support cut views and volumetric exploration, but do not visually clip the inflated surface.";

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
            m_Toggle.interactable = !SelectedScene.IsSurfaceRepresentationTransitioning && (current == SurfaceRepresentation.Inflated || available);
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
                if (RequiresLoadingManager(scene, representation))
                {
                    await LoadingManager.LoadAsync(async (update, token) =>
                    {
                        IProgress<float> progress = new Progress<float>(value => update(value, 0.0f, new LoadingText("Inflating surface")));
                        try
                        {
                            await scene.PrepareSurfaceRepresentationAsync(representation, progress, token);
                        }
                        catch (SurfaceInflationException exception)
                        {
                            throw new HBPException("Surface inflation failed", BuildInflationError(exception));
                        }

                        return true;
                    });
                }

                await scene.SetSurfaceRepresentationAsync(representation, animate: true);
            }
            catch (OperationCanceledException)
            {
            }
            catch (HBPException)
            {
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

        private static bool RequiresLoadingManager(Base3DScene scene, SurfaceRepresentation representation)
        {
            return representation == SurfaceRepresentation.Inflated && !scene.MeshManager.SelectedMesh.HasInflatedRepresentation;
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
                ShowCutsInformationIfNeeded(m_SubscribedScene);
            }
        }

        private void OnSurfaceRepresentationChanged(SurfaceRepresentation representation)
        {
            if (representation == SurfaceRepresentation.Inflated && m_SubscribedScene != null && m_SubscribedScene.Cuts.Count > 0)
            {
                ShowCutsInformationIfNeeded(m_SubscribedScene);
            }
        }

        private static void ShowCutsInformationIfNeeded(Base3DScene scene)
        {
            if (!scene.TryMarkInflatedCutsInformationShown()) return;
            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Cuts in inflated mode", CUTS_INFORMATION, "OK").Forget();
        }

        private static string BuildInflationError(SurfaceInflationException exception)
        {
            SurfaceInflationReport report = exception.Report;
            return $"{exception.Message}\n\nVertices: {report.VertexCount:N0}\nTriangles: {report.TriangleCount:N0}\nComponents: {report.ComponentCount:N0}\nBoundary edges: {report.BoundaryEdgeCount:N0}\nNon-manifold edges: {report.NonManifoldEdgeCount:N0}\nNon-manifold vertices: {report.NonManifoldVertexCount:N0}\nIterations: {report.IterationCount:N0}\nConverged: {(report.Converged ? "yes" : "no")}";
        }

        private void OnDestroy()
        {
            ChangeSceneSubscription(null);
        }
    }
}
