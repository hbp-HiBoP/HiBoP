using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.UI.Tools;

namespace HBP.UI.Toolbar
{
    public class ComputeActivity : Tool
    {
        #region Properties

        /// <summary>
        /// Trigger the computation of the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Button m_Compute;

        /// <summary>
        /// Remove the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Button m_Remove;

        private bool m_ProjectionConfirmationPending;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Compute.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                RequestComputationAsync().Forget();
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.InvalidateActivityField();
                UpdateInteractable();
            });
        }

        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Compute.interactable = false;
            m_Remove.interactable = false;
        }

        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isGeneratorUpToDate = SelectedScene.IsGeneratorUpToDate;

            m_Compute.interactable = SelectedScene.CanComputeFunctionalValues && !m_ProjectionConfirmationPending;
            m_Remove.interactable = isGeneratorUpToDate;
        }

        private async UniTask RequestComputationAsync()
        {
            if (m_ProjectionConfirmationPending) return;

            Base3DScene scene = SelectedScene;
            m_ProjectionConfirmationPending = true;
            UpdateInteractable();
            try
            {
                if (scene.SceneInformation.Initialized)
                    await UniTask.WaitUntil(() => scene == null || scene != SelectedScene || (!scene.SceneInformation.GeometryNeedsUpdate && !scene.SceneInformation.ProjectionGridNeedsUpdate && !scene.SceneInformation.SurfaceProjectionNeedsUpdate));
                if (scene == null || scene != SelectedScene) return;

                while (scene.TryGetSurfaceProjectionWarning(out Core.Enums.DialogBoxType type, out string title, out string message))
                {
                    int projectionGridVersion = scene.ProjectionGridVersion;
                    int surfaceProjectionVersion = scene.SurfaceProjectionVersion;
                    int result = await DialogBoxManager.OpenAsync(type, title, message, "Continue", "Cancel");
                    if (result != 0 || scene == null || scene != SelectedScene) return;

                    if (projectionGridVersion == scene.ProjectionGridVersion && surfaceProjectionVersion == scene.SurfaceProjectionVersion)
                    {
                        scene.AllowCurrentSurfaceProjection();
                        break;
                    }
                }

                scene.InvalidateActivityField();
                scene.SceneInformation.GeneratorUpdateRequested = true;
            }
            finally
            {
                m_ProjectionConfirmationPending = false;
                UpdateInteractable();
            }
        }

        #endregion
    }
}
