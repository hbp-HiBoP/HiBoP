using System;
using System.Collections.Generic;
using System.Globalization;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using UnityEngine;
using UnityEngine.UI;
using AtlasSource = HBP.Data.Module3D.TriangleEraser.AtlasSource;

namespace HBP.Quest
{
    public sealed class QuestTriangleErasingPanel : MonoBehaviour
    {
        internal const float ContactRadius = .003f;
        private const string ProjectionUnavailable = "Activity projection is unavailable on this column.";

        [SerializeField] private QuestWindow window;
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private Transform marker;
        [SerializeField] private QuestDropdown mode, atlas, diFuMoVersion;
        [SerializeField] private Toggle add, remove;
        [SerializeField] private QuestTextInputField angle, activity, threshold, search;
        [SerializeField] private GameObject triangleOptions, angleOptions, activityOptions, atlasOptions;
        [SerializeField] private GameObject regionsPanel, diFuMoWeightOptions;
        [SerializeField] private Button regionsButton, expand, invert, erase, show, keep, clear, undo, reset;
        [SerializeField] private Text status;
        [SerializeField] private RectTransform regionRows;
        [SerializeField] private QuestLabelChoice regionPrefab;

        private readonly List<AtlasSource> atlases = new();
        private readonly List<string> versions = new();
        private readonly List<QuestLabelChoice> rows = new();
        private TriangleEraser.AtlasRegion[] regions = Array.Empty<TriangleEraser.AtlasRegion>();
        private Base3DScene scene;
        private QuestColumnPresentation contactColumn;
        private uint gripReference, regionReference;
        private TriangleSelectionMode gripMode;
        private AtlasSource? listedAtlas;
        private string listedVersion;
        private string feedback;
        private Vector3 previousPosition;
        private int hand = -1;
        private bool hasPosition, seeded, atlasWasComputing, regionsEditable, regionsDirty = true;

        private TriangleEraser Eraser => scene != null ? scene.TriangleEraser : null;
        public bool Ready => view != null && IsReady(view.Scene);
        public bool HasGrip => hand >= 0;
        public bool CapturesGrip => isActiveAndEnabled && window.IsOpen && scene == view.Scene && IsReady(scene) && Eraser.IsEnabled && Eraser.Mode != TriangleSelectionMode.Atlas;

        private bool IsReady(Base3DScene candidate) => QuestAnatomySession.CanInteractWith(candidate) && candidate.IsVisible && !candidate.SceneInformation.GeometryNeedsUpdate && !candidate.IsSurfaceRepresentationTransitioning && candidate.TriangleEraser.HasEditableSurface && view.Find(candidate) is { IsPlaced: true, IsTransitioning: false, SurfaceHidden: false } && SceneReconciliationRecord.Get(candidate)?.Status is not (ReconciliationStatus.Reconciling or ReconciliationStatus.OutOfSync);

        private void Awake()
        {
            mode.ClearOptions();
            mode.AddOptions(new List<string> { "Triangle", "Angle", "Activity", "Atlas" });
            mode.onValueChanged.AddListener(value =>
            {
                if (CanEdit)
                {
                    Eraser.Mode = (TriangleSelectionMode)value;
                    feedback = null;
                    Refresh();
                }
            });
            add.onValueChanged.AddListener(value => SetRemove(value, false));
            remove.onValueChanged.AddListener(value => SetRemove(value, true));
            angle.onEndEdit.AddListener(value => SetNumber(value, 180, number => Eraser.Degrees = Mathf.RoundToInt(number)));
            activity.onEndEdit.AddListener(value => SetNumber(value, 100, number => Eraser.ActivityTolerance = number));
            threshold.onEndEdit.AddListener(value => SetNumber(value, 1, number => Eraser.DiFuMoThreshold = number));
            atlas.onValueChanged.AddListener(_ => regionsDirty = true);
            diFuMoVersion.onValueChanged.AddListener(_ => regionsDirty = true);
            search.onValueChanged.AddListener(_ => regionsDirty = true);
            regionsButton.onClick.AddListener(() =>
            {
                regionsPanel.SetActive(!regionsPanel.activeSelf);
                Refresh();
            });
            expand.onClick.AddListener(() => Edit(value => value.ExpandSelection()));
            invert.onClick.AddListener(() => Edit(value => value.InvertSelection()));
            erase.onClick.AddListener(() => Edit(value => value.ApplySelection(false), visibility: true));
            show.onClick.AddListener(() => Edit(value => value.RestoreSelection(), visibility: true));
            keep.onClick.AddListener(() => Edit(value => value.ApplySelection(true), visibility: true));
            clear.onClick.AddListener(() => Edit(value => value.ClearSelection()));
            undo.onClick.AddListener(() => Edit(value => value.CancelLastAction(), Eraser != null && Eraser.WillUndoVisibility));
            reset.onClick.AddListener(() => Edit(value => value.ResetEraser(), Eraser != null && Eraser.MeshHasInvisibleTriangles));
            window.OnOpened.AddListener(OnOpened);
            window.OnChanged.AddListener(OnWindowChanged);
            marker.gameObject.SetActive(false);
            marker.SetParent(null, false);
            marker.localScale = Vector3.one * (2f * ContactRadius);
            var markerProperties = new MaterialPropertyBlock();
            markerProperties.SetColor("_Color", new Color(.13f, .91f, .99f));
            marker.GetComponent<MeshRenderer>().SetPropertyBlock(markerProperties);
        }

        private void OnOpened(QuestWindow _)
        {
            feedback = null;
            Refresh();
        }

        private void OnWindowChanged(QuestWindow _)
        {
            if (!window.IsOpen) StopEditing();
        }

        private void OnDisable()
        {
            StopEditing();
        }

        private void OnDestroy()
        {
            if (marker != null) Destroy(marker.gameObject);
        }

        private void StopEditing()
        {
            if (HasGrip) EndGrip(hand);
            regionsPanel.SetActive(false);
            if (Eraser != null) Eraser.IsEnabled = false;
        }

        private void Update() => Refresh();

        private bool CanEdit => !HasGrip && window.IsOpen && scene == view.Scene && IsReady(scene) && Eraser.IsEnabled && !Eraser.IsComputingSelection;
        private bool CanCommit => session == null || !session.HasPendingTriangleMask(scene);

        private void Edit(Action<TriangleEraser> command, bool visibility = false)
        {
            if (!CanEdit) return;
            if (visibility)
            {
                if (!CanCommit || !scene.TryBeginSensitiveActivityOperation(out var scope)) return;
                using (scope) command(Eraser);
            }
            else command(Eraser);

            regionsDirty = true;
            Refresh();
        }

        private void SetRemove(bool on, bool value)
        {
            if (on && CanEdit) Eraser.RemoveFromSelection = value;
        }

        private void SetNumber(string text, float maximum, Action<float> setter)
        {
            if (CanEdit && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) && float.IsFinite(number))
                setter(Mathf.Clamp(number, 0, maximum));
        }

        private void Refresh()
        {
            if (view == null || window == null) return;
            if (scene != view.Scene)
            {
                StopEditing();
                scene = view.Scene;
                listedAtlas = null;
                listedVersion = null;
                regions = Array.Empty<TriangleEraser.AtlasRegion>();
                regionsDirty = true;
                feedback = null;
            }

            if (!window.IsOpen || Eraser == null) return;
            bool ready = IsReady(scene);
            if (!ready) StopEditing();
            else Eraser.IsEnabled = true;

            bool editable = ready && !HasGrip && !Eraser.IsComputingSelection;
            bool visibility = editable && CanCommit && !scene.IsActivityProjectionBusy;
            bool selected = Eraser.SelectedCount != 0;
            bool triangleMode = Eraser.Mode == TriangleSelectionMode.Triangle;
            bool atlasMode = Eraser.Mode == TriangleSelectionMode.Atlas;
            mode.SetValueWithoutNotify((int)Eraser.Mode);
            mode.interactable = editable;
            triangleOptions.SetActive(triangleMode);
            angleOptions.SetActive(Eraser.Mode == TriangleSelectionMode.Angle);
            activityOptions.SetActive(Eraser.Mode == TriangleSelectionMode.Activity);
            atlasOptions.SetActive(atlasMode);
            add.SetIsOnWithoutNotify(!Eraser.RemoveFromSelection);
            remove.SetIsOnWithoutNotify(Eraser.RemoveFromSelection);
            add.interactable = remove.interactable = editable;
            RefreshNumber(angle, Eraser.Degrees, editable);
            RefreshNumber(activity, Eraser.ActivityTolerance, editable);
            if (atlasMode) RefreshAtlas(editable);
            else regionsPanel.SetActive(false);
            expand.interactable = clear.interactable = editable && selected;
            invert.interactable = editable && Eraser.CanInvertSelection;
            erase.interactable = visibility && Eraser.SelectedCount > Eraser.SelectedErasedCount;
            show.interactable = visibility && Eraser.SelectedErasedCount > 0;
            keep.interactable = visibility && selected;
            undo.interactable = editable && Eraser.CanCancelLastAction && (!Eraser.WillUndoVisibility || visibility);
            reset.interactable = editable && (Eraser.MeshHasInvisibleTriangles ? visibility : selected);
            status.text = !ready ? "Triangle editing is unavailable in the current view." : Eraser.IsComputingSelection ? "Selecting…" : feedback ?? (Eraser.Mode == TriangleSelectionMode.Activity && !Eraser.CanSelectActivity ? ProjectionUnavailable : atlasMode && atlases.Count == 0 ? "No compatible atlas is loaded." : "");
            status.gameObject.SetActive(status.text.Length > 0);
        }

        private static void RefreshNumber(QuestTextInputField field, float value, bool editable)
        {
            field.interactable = editable;
            if (!field.IsEditing) field.SetTextWithoutNotify(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private void RefreshAtlas(bool editable)
        {
            if (atlasWasComputing != Eraser.IsComputingSelection)
            {
                atlasWasComputing = Eraser.IsComputingSelection;
                regionsDirty = true;
            }

            AtlasSource? previous = atlas.value < atlases.Count ? atlases[atlas.value] : null;
            if (RefreshAtlases())
            {
                atlas.ClearOptions();
                atlas.AddOptions(atlases.ConvertAll(value => value.ToString()));
                atlas.SetValueWithoutNotify(previous.HasValue ? Mathf.Max(0, atlases.IndexOf(previous.Value)) : 0);
            }

            AtlasSource? source = atlases.Count == 0 ? null : atlases[atlas.value];
            bool difumo = source == AtlasSource.DiFuMo;
            string version = null;
            if (difumo)
            {
                var loaded = Eraser.GetLoadedDiFuMoVersions();
                if (!System.Linq.Enumerable.SequenceEqual(versions, loaded))
                {
                    string prior = diFuMoVersion.value < versions.Count ? versions[diFuMoVersion.value] : null;
                    versions.Clear();
                    versions.AddRange(loaded);
                    diFuMoVersion.ClearOptions();
                    diFuMoVersion.AddOptions(versions);
                    diFuMoVersion.SetValueWithoutNotify(Mathf.Max(0, versions.IndexOf(prior)));
                }

                if (versions.Count != 0) version = versions[diFuMoVersion.value];
            }

            atlas.interactable = editable && atlases.Count != 0;
            diFuMoVersion.gameObject.SetActive(difumo);
            diFuMoVersion.interactable = editable && versions.Count != 0;
            if (!source.HasValue) regionsPanel.SetActive(false);
            regionsButton.interactable = regionsPanel.activeSelf || editable && source.HasValue;
            if (!regionsPanel.activeSelf) return;
            diFuMoWeightOptions.SetActive(difumo);
            RefreshNumber(threshold, Eraser.DiFuMoThreshold, editable);
            search.interactable = editable && source.HasValue;

            if (source != listedAtlas || version != listedVersion || Eraser.EditReferenceVersion != regionReference)
            {
                listedAtlas = source;
                listedVersion = version;
                regionReference = Eraser.EditReferenceVersion;
                regions = source.HasValue ? Eraser.GetAtlasRegions(source.Value, version) : Array.Empty<TriangleEraser.AtlasRegion>();
                regionsDirty = true;
            }

            bool refreshInteractability = regionsDirty || regionsEditable != editable;
            if (regionsDirty)
            {
                regionsDirty = false;
                int index = 0;
                foreach (var region in regions)
                {
                    if (region.Name.IndexOf(search.text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (index == rows.Count) rows.Add(Instantiate(regionPrefab, regionRows));
                    var row = rows[index++];
                    row.gameObject.SetActive(true);
                    int id = region.Id;
                    row.Bind(region.Name, Eraser.IsAtlasRegionSelected(source.Value, version, id), (_, check) => { Edit(value => value.SetAtlasRegionSelected(source.Value, version, id, check)); });
                }

                for (int i = index; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            }

            if (refreshInteractability)
            {
                regionsEditable = editable;
                foreach (var row in rows)
                    if (row.gameObject.activeSelf)
                        row.SetInteractable(editable);
            }
        }

        private bool RefreshAtlases()
        {
            int index = 0;
            bool changed = false;
            for (var source = AtlasSource.MarsAtlas; source <= AtlasSource.DiFuMo; source++)
            {
                if (!Eraser.IsAtlasAvailable(source)) continue;
                if (index == atlases.Count)
                {
                    atlases.Add(source);
                    changed = true;
                }
                else if (atlases[index] != source)
                {
                    atlases[index] = source;
                    changed = true;
                }

                index++;
            }

            if (index == atlases.Count) return changed;
            atlases.RemoveRange(index, atlases.Count - index);
            return true;
        }

        internal bool IsCaptured(int owner) => hand == owner && scene != null && Eraser.EditReferenceVersion == gripReference;
        internal bool IsCapturedInScene(int owner, Base3DScene candidate) => hand == owner && scene == candidate;
        internal bool IsRegionScroll(ScrollRect scroll) => scroll.content == regionRows;

        internal bool BeginGrip(int owner)
        {
            if (!CapturesGrip || HasGrip || Eraser.IsComputingSelection) return false;
            gripMode = Eraser.Mode;
            if (gripMode == TriangleSelectionMode.Triangle && !Eraser.BeginTriangleStroke()) return false;
            hand = owner;
            gripReference = Eraser.EditReferenceVersion;
            contactColumn = null;
            hasPosition = seeded = false;
            feedback = null;
            return true;
        }

        internal bool MoveGrip(int owner, Vector3 position)
        {
            if (!IsCaptured(owner) || !CapturesGrip || contactColumn != null && !CanContact(contactColumn)) return false;
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return false;
            marker.position = position;
            marker.gameObject.SetActive(true);

            if (!seeded)
            {
                float travel = hasPosition ? Vector3.Distance(previousPosition, position) : 0f;
                int samples = Mathf.Max(1, Mathf.CeilToInt(travel / (.5f * ContactRadius)));
                for (int sample = 1; sample <= samples && !seeded; sample++)
                    Contact(Vector3.Lerp(previousPosition, position, (float)sample / samples));
            }

            previousPosition = position;
            hasPosition = true;
            Eraser.FlushTriangleStrokePreview();
            return true;
        }

        private bool CanContact(QuestColumnPresentation column) => view.CanInteractWithColumn(column) && !column.Manipulator.IsGrabbed && column.Scene == scene && view.Find(scene)?.SurfaceHidden == false;

        private void Contact(Vector3 position)
        {
            float distance = ContactRadius;
            int face = -1;
            if (contactColumn != null)
                Eraser.TryContact(contactColumn.Column, position, ref distance, out face);
            else
            {
                QuestColumnPresentation closest = null;
                foreach (var column in view.Columns)
                    if (CanContact(column) && Eraser.TryContact(column.Column, position, ref distance, out int candidate))
                    {
                        face = candidate;
                        closest = column;
                    }

                contactColumn = closest;
            }

            if (face < 0) return;
            if (gripMode == TriangleSelectionMode.Triangle)
            {
                Eraser.RetouchTriangleStroke(face);
                return;
            }

            seeded = true;
            if (gripMode == TriangleSelectionMode.Activity && (!Eraser.CanSelectActivity || contactColumn.Column.ActivityGenerator?.ProjectionGrid == null))
                feedback = ProjectionUnavailable;
            else Eraser.SelectTriangle(contactColumn.Column, face);
        }

        internal void EndGrip(int owner)
        {
            if (hand != owner) return;
            if (IsCaptured(owner)) Eraser.EndTriangleStroke();
            hand = -1;
            contactColumn = null;
            if (marker != null) marker.gameObject.SetActive(false);
        }
    }
}
