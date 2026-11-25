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
        private const float TriangleMarkerRadius = .00075f;
        private static readonly Color SelectingColor = new(.13f, .91f, .99f);
        private const string ProjectionUnavailable = "Activity projection is unavailable on this column.";

        [SerializeField] private QuestWindow window;
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private Transform marker, secondMarker;
        [SerializeField] private HBP.Theme.Color markerIdleColor;
        [SerializeField] private QuestDropdown mode, atlas, diFuMoVersion;
        [SerializeField] private Toggle add, remove;
        [SerializeField] private QuestTextInputField angle, activity, threshold, search, brushRadius;
        [SerializeField] private GameObject triangleOptions, angleOptions, activityOptions, atlasOptions, brushOptions;
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
        private uint regionReference;
        private AtlasSource? listedAtlas;
        private string listedVersion;
        private string feedback;
        private bool atlasWasComputing, regionsEditable, regionsDirty = true;
        private readonly Grip[] grips = new Grip[2];
        private readonly Grip[] activityRequests = new Grip[2];
        private Transform[] markers;
        private MeshRenderer[] markerRenderers;
        private MaterialPropertyBlock markerProperties;

        private sealed class Grip
        {
            public uint Reference;
            public TriangleSelectionMode Mode;
            public float Radius;
            public QuestColumnPresentation Column;
            public Vector3 PreviousPosition;
            public bool HasPosition, Seeded, Cancelled;
        }

        private TriangleEraser Eraser => scene != null ? scene.TriangleEraser : null;
        public bool Ready => view != null && IsReady(view.Scene);
        public bool HasGrip => grips[0] != null || grips[1] != null;
        public bool CapturesGrip => isActiveAndEnabled && window.IsOpen && scene == view.Scene && IsReady(scene) && Eraser.IsEnabled && Eraser.Mode != TriangleSelectionMode.Atlas;

        private bool IsReady(Base3DScene candidate) => QuestAnatomySession.CanInteractWith(candidate) && candidate.IsVisible && !candidate.SceneInformation.GeometryNeedsUpdate && !candidate.IsSurfaceRepresentationTransitioning && candidate.TriangleEraser.HasEditableSurface && view.Find(candidate) is { IsPlaced: true, IsTransitioning: false, SurfaceHidden: false } && SceneReconciliationRecord.Get(candidate)?.Status is not (ReconciliationStatus.Reconciling or ReconciliationStatus.OutOfSync);

        private void Awake()
        {
            mode.ClearOptions();
            mode.AddOptions(new List<string> { "Triangle", "Angle", "Activity", "Atlas", "Brush" });
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
            if (brushRadius != null)
                brushRadius.onEndEdit.AddListener(value => SetNumber(value, float.MaxValue, number => Eraser.BrushRadiusMillimeters = number));
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
            markers = new[] { marker, secondMarker };
            markerRenderers = new MeshRenderer[markers.Length];
            markerProperties = new MaterialPropertyBlock();
            for (int hand = 0; hand < markers.Length; hand++)
            {
                if (markers[hand] == null) continue;
                markers[hand].gameObject.SetActive(false);
                markers[hand].SetParent(null, false);
                markerRenderers[hand] = markers[hand].GetComponent<MeshRenderer>();
                UpdateMarkerColor(hand);
            }
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
            if (markers == null) return;
            foreach (var handMarker in markers)
                if (handMarker != null)
                    Destroy(handMarker.gameObject);
        }

        private void StopEditing()
        {
            for (int hand = 0; hand < grips.Length; hand++)
            {
                EndGrip(hand, cancelled: true);
                if (markers?[hand] != null) markers[hand].gameObject.SetActive(false);
            }

            if (Eraser != null) Eraser.EndTriangleStroke(discardPending: true);
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
            if (!HasGrip && brushRadius != null && Eraser != null && Eraser.Mode == TriangleSelectionMode.Brush)
                brushRadius.SetTextWithoutNotify(Eraser.BrushRadiusMillimeters.ToString("G", CultureInfo.InvariantCulture));
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
            bool brushMode = Eraser.Mode == TriangleSelectionMode.Brush;
            bool atlasMode = Eraser.Mode == TriangleSelectionMode.Atlas;
            mode.SetValueWithoutNotify((int)Eraser.Mode);
            mode.interactable = editable;
            triangleOptions.SetActive(Eraser.IsSweepMode);
            if (brushOptions != null) brushOptions.SetActive(brushMode);
            angleOptions.SetActive(Eraser.Mode == TriangleSelectionMode.Angle);
            activityOptions.SetActive(Eraser.Mode == TriangleSelectionMode.Activity);
            atlasOptions.SetActive(atlasMode);
            add.SetIsOnWithoutNotify(!Eraser.RemoveFromSelection);
            remove.SetIsOnWithoutNotify(Eraser.RemoveFromSelection);
            add.interactable = remove.interactable = editable;
            RefreshNumber(angle, Eraser.Degrees, editable);
            RefreshNumber(activity, Eraser.ActivityTolerance, editable);
            if (brushRadius != null)
            {
                brushRadius.interactable = editable;
                if (!brushRadius.IsEditing)
                    brushRadius.SetTextWithoutNotify(Eraser.BrushRadiusMillimeters.ToString("G", CultureInfo.InvariantCulture));
            }

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

        internal bool IsCaptured(int owner) => grips[owner] is { } grip && Eraser != null && Eraser.EditReferenceVersion == grip.Reference;
        internal bool IsCapturedInScene(int owner, Base3DScene candidate) => scene == candidate && (grips[owner] != null || activityRequests[owner] != null);
        internal bool IsRegionScroll(ScrollRect scroll) => scroll.content == regionRows;

        internal void UpdateMarker(int owner, bool tracked, Vector3 position)
        {
            var handMarker = markers?[owner];
            if (handMarker == null) return;
            bool show = tracked && CapturesGrip && IsFinite(position);
            if (show)
            {
                var grip = grips[owner];
                float radius = (grip?.Mode ?? Eraser.Mode) switch
                {
                    TriangleSelectionMode.Triangle => TriangleMarkerRadius,
                    TriangleSelectionMode.Brush => grip?.Radius ?? Eraser.BrushRadiusMillimeters * .001f,
                    _ => ContactRadius
                };
                handMarker.position = position;
                handMarker.localScale = Vector3.one * (2f * radius);
            }

            if (handMarker.gameObject.activeSelf != show) handMarker.gameObject.SetActive(show);
        }

        private void UpdateMarkerColor(int owner)
        {
            var renderer = markerRenderers?[owner];
            if (renderer == null) return;
            markerProperties.SetColor("_Color", grips[owner] != null ? SelectingColor : markerIdleColor != null ? markerIdleColor.Value : new Color(.22f, .44f, .69f));
            renderer.SetPropertyBlock(markerProperties);
        }

        internal bool CanBeginGrip(int owner) => CapturesGrip && grips[owner] == null && activityRequests[owner] == null && (HasGrip ? Eraser.CanJoinTriangleStroke : !Eraser.HasTriangleStroke && !Eraser.IsComputingSelection);

        internal bool BeginGrip(int owner)
        {
            if (!CanBeginGrip(owner) || !HasGrip && !Eraser.BeginTriangleStroke()) return false;
            grips[owner] = new Grip
            {
                Mode = Eraser.Mode,
                Reference = Eraser.EditReferenceVersion,
                Radius = Eraser.StrokeBrushRadiusMillimeters * .001f
            };
            feedback = null;
            UpdateMarkerColor(owner);
            return true;
        }

        internal bool MoveGrip(int owner, Vector3 position)
        {
            if (!IsCaptured(owner) || !CapturesGrip || !IsFinite(position)) return false;
            var grip = grips[owner];
            if (grip.Column != null && !CanContact(grip.Column)) return false;
            var start = grip.HasPosition ? grip.PreviousPosition : position;
            if (grip.Mode == TriangleSelectionMode.Brush)
            {
                if (!grip.HasPosition || !start.Equals(position))
                {
                    grip.Column ??= FindBrushColumn(start, position, grip.Radius);
                    if (grip.Column != null) Eraser.RetouchBrushStroke(grip.Column.Column, start, position, displayedUnits: false);
                }
            }
            else if (!grip.Seeded)
            {
                float travel = Vector3.Distance(start, position);
                int samples = Mathf.Max(1, Mathf.CeilToInt(travel / (.5f * ContactRadius)));
                for (int sample = 1; sample <= samples && !grip.Seeded; sample++)
                    Contact(owner, grip, Vector3.Lerp(start, position, (float)sample / samples));
            }

            grip.PreviousPosition = position;
            grip.HasPosition = true;
            return true;
        }

        private static bool IsFinite(Vector3 position) => float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z);

        private bool CanContact(QuestColumnPresentation column) => view.CanInteractWithColumn(column) && !column.Manipulator.IsGrabbed && column.Scene == scene && view.Find(scene)?.SurfaceHidden == false;

        private QuestColumnPresentation FindBrushColumn(Vector3 start, Vector3 end, float radius)
        {
            // Prefer the closest surface when the sphere initially touches more than one column.
            var initial = FindColumn(start, radius, out _);
            if (initial != null || start.Equals(end)) return initial;

            QuestColumnPresentation first = null;
            float earliest = 1f;
            foreach (var column in view.Columns)
            {
                if (!CanContact(column) || !Eraser.HasBrushContact(column.Column, start, Vector3.Lerp(start, end, earliest), radius))
                    continue;

                // Overlap is monotonic for prefixes of a sweep. Refine the first time only until a column is locked.
                float low = 0f, high = earliest;
                for (int step = 0; step < 16; step++)
                {
                    float middle = (low + high) * .5f;
                    if (Eraser.HasBrushContact(column.Column, start, Vector3.Lerp(start, end, middle), radius)) high = middle;
                    else low = middle;
                }

                if (first != null && high >= earliest) continue;
                earliest = high;
                first = column;
            }

            return first;
        }

        private QuestColumnPresentation FindColumn(Vector3 position, float distance, out int face)
        {
            face = -1;
            QuestColumnPresentation closest = null;
            foreach (var column in view.Columns)
                if (CanContact(column) && Eraser.TryContact(column.Column, position, ref distance, out int candidate))
                {
                    face = candidate;
                    closest = column;
                }

            return closest;
        }

        private void Contact(int owner, Grip grip, Vector3 position)
        {
            float distance = ContactRadius;
            int face;
            if (grip.Column == null) grip.Column = FindColumn(position, distance, out face);
            else Eraser.TryContact(grip.Column.Column, position, ref distance, out face);
            if (face < 0) return;
            if (grip.Mode == TriangleSelectionMode.Triangle)
            {
                Eraser.RetouchTriangleStroke(face);
                return;
            }

            grip.Seeded = true;
            bool activity = grip.Mode == TriangleSelectionMode.Activity;
            if (activity) activityRequests[owner] = grip;
            if (!Eraser.RetouchStrokeSeed(grip.Column.Column, face, () => !grip.Cancelled, () => activityRequests[owner] = null) && activity)
            {
                activityRequests[owner] = null;
                feedback = ProjectionUnavailable;
            }
        }

        internal void EndGrip(int owner, bool cancelled = false)
        {
            if (cancelled && activityRequests[owner] is { } activity) activity.Cancelled = true;
            if (grips[owner] == null) return;
            bool current = IsCaptured(owner);
            grips[owner] = null;
            if (current && !HasGrip) Eraser.EndTriangleStroke();
            UpdateMarkerColor(owner);
        }
    }
}
