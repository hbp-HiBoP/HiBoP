using System;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Optional Desktop view layout. Removing this component does not close the content.
    /// Scientific state and rendering remain owned by the scene and its columns.
    /// </summary>
    public sealed class DesktopScenePresentation : MonoBehaviour
    {
        #region Properties

        [SerializeField] private Base3DScene m_Scene;
        [SerializeField] private GameObject m_ViewPrefab;

        #endregion

        #region Public Methods

        public void Initialize()
        {
            transform.position = new Vector3(Module3DMain.SPACE_BETWEEN_SCENES_GAME_OBJECTS * Module3DMain.NumberOfScenesLoadedSinceStart++, transform.position.y, transform.position.z);
        }

        public string GetColumnLayer(int index) => "Column" + index;

        public void InitializeColumn(Column3D column)
        {
            column.OnMoveView.AddListener(SynchronizeViews);
            column.OnSelect.AddListener(DeselectOtherViews);
            AddView(column);
        }

        public void UpdateViews(Action<View3D> update)
        {
            foreach (var column in m_Scene.Columns)
            {
                foreach (var view in column.Views)
                    update(view);
            }
        }

        public void AddView(Column3D column)
        {
            var view = Instantiate(m_ViewPrefab, column.ViewsParent).GetComponent<View3D>();
            view.Initialize(column.Views.Count, column.Layer);
            view.OnSelect.AddListener(() =>
            {
                foreach (var other in column.Views)
                    if (other != view)
                        other.IsSelected = false;
                column.IsSelected = true;
            });
            view.OnMoveView.AddListener(() => column.OnMoveView.Invoke(view));
            column.Views.Add(view);
            view.ShowEdges = m_Scene.EdgeMode;
            view.AutomaticRotation = m_Scene.AutomaticRotation;
            view.AutomaticRotationSpeed = m_Scene.AutomaticRotationSpeed;
            view.CameraType = m_Scene.CameraType;
        }

        public void AddViewLine()
        {
            foreach (var column in m_Scene.Columns) AddView(column);
            m_Scene.OnAddViewLine.Invoke();
        }

        public void RemoveViewLine(int lineID = -1)
        {
            if (m_Scene.ViewLineNumber <= 1) return;
            if (lineID == -1) lineID = m_Scene.ViewLineNumber - 1;
            bool selected = false;
            foreach (var column in m_Scene.Columns)
            {
                selected |= column.Views[lineID].IsSelected;
                column.RemoveView(lineID);
            }

            m_Scene.OnRemoveViewLine.Invoke(m_Scene.ViewLineNumber);
            if (selected) m_Scene.SelectedColumn.Views.First().IsSelected = true;
        }

        public void SelectDefaultView()
        {
            var view = m_Scene.Columns.FirstOrDefault()?.Views.FirstOrDefault();
            if (view) view.IsSelected = true;
        }

        public void LoadConfiguration()
        {
            for (int i = 0; i < m_Scene.Visualization.Configuration.Views.Count; i++)
            {
                View configuration = m_Scene.Visualization.Configuration.Views[i];
                if (i != 0) AddViewLine();
                foreach (var column in m_Scene.Columns)
                    column.Views.Last().SetCamera(configuration.Position.ToVector3(), configuration.Rotation.ToQuaternion(), configuration.Target.ToVector3());
            }
        }

        public void SaveConfiguration()
        {
            m_Scene.Visualization.Configuration.Views = m_Scene.Columns.Take(1).SelectMany(column => column.Views).Select(view => new View(view.LocalCameraPosition, view.LocalCameraRotation, view.LocalCameraTarget)).ToList();
        }

        public void ResetConfiguration()
        {
            while (m_Scene.ViewLineNumber > 1) RemoveViewLine();
            UpdateViews(view => view.Default());
        }

        public void MoveSelectedROISphere(Camera camera, Vector3 screenDelta)
        {
            var sphere = m_Scene.ROIManager.SelectedROI?.SelectedSphere;
            if (!sphere) return;
            Vector3 position = camera.WorldToScreenPoint(sphere.transform.position);
            Vector3 worldDelta = camera.ScreenToWorldPoint(position + screenDelta) - sphere.transform.position;
            m_Scene.ROIManager.MoveSelectedROISphere(sphere.transform.parent.InverseTransformVector(worldDelta));
        }

        public void PassiveRaycastOnScene(Ray ray, Column3D column)
        {
            var atlas = m_Scene.AtlasManager;
            var screenPosition = HBP.Input.DesktopInput.MousePosition;
            if (m_Scene.IsSurfaceRepresentationTransitioning)
            {
                Module3DMain.OnDisplayAtlasInformation.Invoke(atlas.GetAtlasInformation(false, default, screenPosition));
                return;
            }

            bool full = m_Scene.TriangleEraser.MeshHasInvisibleTriangles || atlas.UsesInflatedHoverGeometry;
            RaycastHitResult raycastResult = m_Scene.RaycastDesktop(ray, column, full, out RaycastHit hit, out _, out Vector3 point);
            bool atlasHit = m_Scene.IsBrainRaycastGeometryCurrent && raycastResult is RaycastHitResult.Cut or RaycastHitResult.Mesh;
            Module3DMain.OnDisplayAtlasInformation.Invoke(atlas.GetAtlasInformation(atlasHit, point, screenPosition));
            Module3DMain.OnDisplaySiteInformation.Invoke(m_Scene.ImplantationManager.GetSiteInformation(raycastResult == RaycastHitResult.Site ? hit.collider.GetComponent<HBP.Core.Object3D.Site>() : null, column, screenPosition));
        }

        #endregion

        #region Private Methods

        private void DeselectOtherViews()
        {
            foreach (var column in m_Scene.Columns.Where(column => !column.IsSelected))
            {
                foreach (var view in column.Views)
                    view.IsSelected = false;
            }
        }

        private void SynchronizeViews(View3D reference)
        {
            foreach (var column in m_Scene.Columns)
            {
                foreach (var view in column.Views)
                    if (view.LineID == reference.LineID)
                        view.SynchronizeCamera(reference);
            }
        }

        private void OnDestroy()
        {
            if (!m_Scene) return;
            foreach (var column in m_Scene.Columns)
            {
                column.OnMoveView.RemoveListener(SynchronizeViews);
                column.OnSelect.RemoveListener(DeselectOtherViews);
                foreach (var view in column.Views)
                    if (view)
                        Destroy(view.gameObject);
                column.Views.Clear();
            }
        }

        #endregion
    }
}
