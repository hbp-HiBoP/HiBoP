using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class TriangleErasingMode : Tool
    {
        public static ReadOnlyCollection<TriangleSelectionMode> DropdownModes { get; } = Array.AsReadOnly(new[]
        {
            TriangleSelectionMode.Triangle,
            TriangleSelectionMode.Brush,
            TriangleSelectionMode.Angle,
            TriangleSelectionMode.Activity,
            TriangleSelectionMode.Atlas
        });

        [SerializeField] private Dropdown m_Dropdown;
        [SerializeField] private Toggle m_Add;
        [SerializeField] private Toggle m_Remove;
        [SerializeField] private InputField m_Angle;
        [SerializeField] private InputField m_Activity;
        [SerializeField] private InputField m_BrushRadius;
        [SerializeField] private Button m_EraseSelection;
        [SerializeField] private Button m_KeepSelection;
        [SerializeField] private Button m_RestoreSelection;
        [SerializeField] private Button m_ClearSelection;

        [SerializeField] private Dropdown m_Atlas;
        [SerializeField] private Dropdown m_DiFuMoVersion;
        [SerializeField] private DropWindow m_Regions;
        [SerializeField] private Button m_RegionsButton;
        [SerializeField] private InputField m_Search;
        [SerializeField] private InputField m_DiFuMoThreshold;
        [SerializeField] private ScrollRect m_Scroll;
        [SerializeField] private Toggle m_RegionTogglePrefab;

        public const string NotAvailable = "Not available";
        public const string NoActivityProjection = "No activity projection on this column";
        public const string ComputingActivityProjection = "Computing activity projection…";
        public const string NoCompatibleAtlas = "No compatible atlas for this surface";

        private readonly List<TriangleEraser.AtlasSource> m_AvailableAtlases = new();
        private TriangleEraser.AtlasSource[] m_LoadedAtlases;
        private TriangleEraser.AtlasSource? m_SelectedAtlas;
        private string[] m_LoadedVersions = Array.Empty<string>();
        private string m_SelectedVersion;
        private (Base3DScene Scene, Surface Surface, long Geometry, TriangleEraser.AtlasSource Atlas, string Version)? m_ListContext;
        private readonly List<(Toggle Toggle, TriangleEraser.AtlasRegion Region)> m_RegionRows = new();
        private Tooltip m_ClearTooltip, m_AtlasTooltip;
        private DropdownPopupOptions m_ModePopup, m_AtlasPopup, m_VersionPopup;
        private Base3DScene m_ContextScene;
        private Column3D m_ContextColumn;
        private uint m_ContextReference;
        private bool m_ActivityAvailable, m_AtlasAvailable, m_UpdatingContext;

        private bool HasCurrentContext => !m_UpdatingContext && m_ContextScene && m_ContextScene == SelectedScene && m_ContextColumn == SelectedColumn &&
            m_ContextReference == SelectedScene.TriangleEraser.EditReferenceVersion;

        public override void Initialize()
        {
            m_ModePopup = DropdownPopupOptions.Attach(m_Dropdown, ModeUnavailableReason, showTooltip: true);
            m_AtlasPopup = DropdownPopupOptions.Attach(m_Atlas);
            m_VersionPopup = DropdownPopupOptions.Attach(m_DiFuMoVersion);
            m_AtlasTooltip = m_Atlas.GetComponent<Tooltip>() ?? m_Atlas.gameObject.AddComponent<Tooltip>();
            m_Dropdown.onValueChanged.AddListener(value =>
            {
                if (ListenerLock || !SelectedScene || (uint)value >= (uint)DropdownModes.Count) return;
                if (!HasCurrentContext || !m_Dropdown.interactable || ModeUnavailableReason(value) != null)
                {
                    m_Dropdown.SetValueWithoutNotify(DropdownModes.IndexOf(SelectedScene.TriangleEraser.Mode));
                    return;
                }

                SelectedScene.TriangleEraser.Mode = DropdownModes[value];
                UpdateStatus();
                UpdateInteractable();
            });
            m_Add.onValueChanged.AddListener(value =>
            {
                if (value && !ListenerLock && HasCurrentContext) SelectedScene.TriangleEraser.RemoveFromSelection = false;
            });
            m_Remove.onValueChanged.AddListener(value =>
            {
                if (value && !ListenerLock && HasCurrentContext) SelectedScene.TriangleEraser.RemoveFromSelection = true;
            });
            m_Angle.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.Degrees = Mathf.Clamp(Mathf.RoundToInt(number), 0, 180)));
            m_Activity.onEndEdit.AddListener(value =>
            {
                if (m_Activity.interactable && HasCurrentContext && SelectedScene.TriangleEraser.CanSelectActivity(SelectedColumn))
                    SetNumber(value, (eraser, number) => eraser.ActivityTolerance = Mathf.Clamp(number, 0f, 100f));
            });
            m_BrushRadius.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.BrushRadiusMillimeters = number));
            m_DiFuMoThreshold.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.DiFuMoThreshold = Mathf.Clamp01(number)));
            m_EraseSelection.onClick.AddListener(() => Apply(false));
            m_KeepSelection.onClick.AddListener(() => Apply(true));
            m_RestoreSelection.onClick.AddListener(() =>
            {
                if (!ListenerLock && HasCurrentContext) SelectedScene.TriangleEraser.RestoreSelection();
            });
            m_ClearTooltip = m_ClearSelection.GetComponent<Tooltip>();
            m_ClearSelection.onClick.AddListener(() =>
            {
                if (!ListenerLock && HasCurrentContext) SelectedScene.TriangleEraser.ClearSelection();
            });

            m_Atlas.onValueChanged.AddListener(value =>
            {
                if (ListenerLock || !HasCurrentContext || !m_Atlas.interactable || m_LoadedAtlases == null ||
                    (uint)value >= (uint)m_LoadedAtlases.Length) return;
                var eraser = SelectedScene.TriangleEraser;
                if (!eraser.IsAtlasAvailable(m_LoadedAtlases[value])) return;
                m_SelectedAtlas = m_LoadedAtlases[value];
                UpdateAtlasOptions(eraser);
            });
            m_DiFuMoVersion.onValueChanged.AddListener(value =>
            {
                if (ListenerLock || !HasCurrentContext || !m_DiFuMoVersion.interactable ||
                    m_SelectedAtlas != TriangleEraser.AtlasSource.DiFuMo || (uint)value >= (uint)m_LoadedVersions.Length) return;
                var eraser = SelectedScene.TriangleEraser;
                if (!eraser.IsAtlasAvailable(TriangleEraser.AtlasSource.DiFuMo) ||
                    !eraser.GetLoadedDiFuMoVersions().Contains(m_LoadedVersions[value])) return;
                m_SelectedVersion = m_LoadedVersions[value];
                UpdateAtlasOptions(eraser);
            });
            m_Regions.OnChangeWindowState.AddListener(open =>
            {
                if (open && HasCurrentContext && m_RegionsButton.interactable) PopulateRegions(SelectedScene.TriangleEraser);
            });
            m_Search.onValueChanged.AddListener(FilterRegions);
        }

        private void SetNumber(string value, Action<TriangleEraser, float> update)
        {
            if (ListenerLock || !HasCurrentContext) return;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) && float.IsFinite(number))
                update(SelectedScene.TriangleEraser, number);
            UpdateStatus();
        }

        private void Apply(bool keep)
        {
            if (!ListenerLock && HasCurrentContext) SelectedScene.TriangleEraser.ApplySelection(keep);
        }

        private void OnEnable()
        {
            AtlasResources.OnChanged.AddListener(OnAtlasChanged);
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private void OnDisable()
        {
            AtlasResources.OnChanged.RemoveListener(OnAtlasChanged);
            SetContext(null, null, 0);
            CloseRegions();
        }

        private void OnAtlasChanged(AtlasLoadResult _) => Module3DMain.OnRequestUpdateInToolbar.Invoke();

        private void OnProjectionChanged(bool _)
        {
            if (isActiveAndEnabled) Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        internal void InvalidateScene(Base3DScene selected)
        {
            if (m_ContextScene && m_ContextScene != selected) DefaultState();
        }

        private void SetContext(Base3DScene scene, Column3D column, uint reference)
        {
            bool sceneChanged = m_ContextScene != scene;
            bool surfaceChanged = sceneChanged || m_ContextReference != reference;
            if (!surfaceChanged && m_ContextColumn == column) return;

            m_UpdatingContext = true;
            m_ModePopup?.Close();
            if (m_Activity && m_Activity.isFocused) m_Activity.DeactivateInputField();
            if (surfaceChanged)
            {
                m_AtlasPopup?.Close();
                m_VersionPopup?.Close();
                if (m_DiFuMoThreshold && m_DiFuMoThreshold.isFocused) m_DiFuMoThreshold.DeactivateInputField();
                CloseRegions();
                m_ListContext = null;
            }

            if (sceneChanged)
            {
                if (m_Angle && m_Angle.isFocused) m_Angle.DeactivateInputField();
                if (m_BrushRadius && m_BrushRadius.isFocused) m_BrushRadius.DeactivateInputField();
                if (m_ContextScene)
                {
                    m_ContextScene.OnActivityProjectionBusyChanged.RemoveListener(OnProjectionChanged);
                    m_ContextScene.OnIEEGOutdated.RemoveListener(OnProjectionChanged);
                }

                if (scene)
                {
                    scene.OnActivityProjectionBusyChanged.AddListener(OnProjectionChanged);
                    scene.OnIEEGOutdated.AddListener(OnProjectionChanged);
                }
            }

            m_ContextScene = scene;
            m_ContextColumn = column;
            m_ContextReference = reference;
            m_UpdatingContext = false;
        }

        public override void DefaultState()
        {
            SetContext(null, null, 0);
            m_Dropdown.interactable = false;
            RefreshAtlasChoices(null);
            ShowOptions(null);
            SetSelectionButtons(0, 0, false);
        }

        public override void UpdateInteractable()
        {
            TriangleEraser eraser = SelectedScene.TriangleEraser;
            bool ready = !eraser.IsComputingSelection && !eraser.HasTriangleStroke;
            m_Dropdown.interactable = ready;
            m_Add.interactable = ready;
            m_Remove.interactable = ready;
            m_Angle.interactable = ready;
            m_BrushRadius.interactable = ready;
            SetSelectionButtons(eraser.SelectedCount, eraser.SelectedErasedCount, ready);
        }

        private void SetSelectionButtons(int count, int erased, bool ready)
        {
            m_EraseSelection.interactable = m_KeepSelection.interactable = m_ClearSelection.interactable = ready && count > 0;
            m_RestoreSelection.interactable = ready && erased > 0;
            if (m_ClearTooltip)
                m_ClearTooltip.Text = count == 0 ? "Deselect all" : $"Deselect all ({count:N0} triangles)";
        }

        public override void UpdateStatus()
        {
            TriangleEraser eraser = SelectedScene.TriangleEraser;
            SetContext(SelectedScene, SelectedColumn, eraser.EditReferenceVersion);
            RefreshAtlasChoices(eraser);
            bool activity = eraser.CanSelectActivity(SelectedColumn);
            bool atlas = m_LoadedAtlases.Length > 0;
            if (activity != m_ActivityAvailable || atlas != m_AtlasAvailable) m_ModePopup?.Close();
            m_ActivityAvailable = activity;
            m_AtlasAvailable = atlas;
            m_Dropdown.SetValueWithoutNotify(DropdownModes.IndexOf(eraser.Mode));
            if (eraser.IsSweepMode)
            {
                Toggle enabled = eraser.RemoveFromSelection ? m_Remove : m_Add;
                Toggle disabled = eraser.RemoveFromSelection ? m_Add : m_Remove;
                enabled.SetIsOnWithoutNotify(true);
                disabled.SetIsOnWithoutNotify(false);
            }

            ShowOptions(eraser.Mode);
            m_Activity.interactable = activity && !eraser.IsComputingSelection && !eraser.HasTriangleStroke;
            SetNumberText(m_Angle, eraser.Degrees);
            SetNumberText(m_Activity, eraser.ActivityTolerance);
            if (!m_BrushRadius.isFocused)
                m_BrushRadius.SetTextWithoutNotify(eraser.BrushRadiusMillimeters.ToString("G", CultureInfo.InvariantCulture));
            if (eraser.Mode == TriangleSelectionMode.Atlas) UpdateAtlasOptions(eraser);
        }

        private string ModeUnavailableReason(int index)
        {
            if (!HasCurrentContext || (uint)index >= (uint)DropdownModes.Count) return NotAvailable;
            var eraser = SelectedScene.TriangleEraser;
            return DropdownModes[index] switch
            {
                TriangleSelectionMode.Activity when !eraser.CanSelectActivity(SelectedColumn) =>
                    SelectedScene.IsActivityProjectionBusy ? ComputingActivityProjection : NoActivityProjection,
                TriangleSelectionMode.Atlas when !m_LoadedAtlases.Any(eraser.IsAtlasAvailable) => NoCompatibleAtlas,
                _ => null
            };
        }

        private void ShowOptions(TriangleSelectionMode? mode)
        {
            bool sweep = mode is TriangleSelectionMode.Triangle or TriangleSelectionMode.Brush;
            m_Add.gameObject.SetActive(sweep);
            m_Remove.gameObject.SetActive(sweep);
            m_Angle.gameObject.SetActive(mode == TriangleSelectionMode.Angle);
            m_Activity.gameObject.SetActive(mode == TriangleSelectionMode.Activity);
            m_BrushRadius.gameObject.SetActive(mode == TriangleSelectionMode.Brush);
            bool atlas = mode == TriangleSelectionMode.Atlas;
            if (!atlas)
            {
                CloseRegions();
                m_DiFuMoVersion.gameObject.SetActive(false);
                m_DiFuMoThreshold.gameObject.SetActive(false);
            }

            m_Atlas.gameObject.SetActive(atlas);
            m_Regions.gameObject.SetActive(atlas && m_LoadedAtlases.Length > 0);
        }

        private static void SetNumberText(InputField field, float value)
        {
            if (!field.isFocused) field.SetTextWithoutNotify(value.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private void RefreshAtlasChoices(TriangleEraser eraser)
        {
            if (eraser) eraser.GetAvailableAtlases(m_AvailableAtlases);
            else m_AvailableAtlases.Clear();
            if (m_LoadedAtlases != null && m_AvailableAtlases.SequenceEqual(m_LoadedAtlases)) return;

            m_AtlasPopup?.Close();
            if (m_SelectedAtlas.HasValue && !m_AvailableAtlases.Contains(m_SelectedAtlas.Value)) CloseRegions();
            m_LoadedAtlases = m_AvailableAtlases.ToArray();
            m_Atlas.ClearOptions();
            m_Atlas.AddOptions(m_LoadedAtlases.Length == 0 ? new List<string> { NotAvailable } : m_AvailableAtlases.Select(atlas => atlas.ToString()).ToList());
            m_Atlas.interactable = m_LoadedAtlases.Length > 0;
            m_AtlasTooltip.Text = m_LoadedAtlases.Length == 0 ? NoCompatibleAtlas : "";
        }

        private void UpdateAtlasOptions(TriangleEraser eraser)
        {
            if (m_LoadedAtlases.Length != 0 && (!m_SelectedAtlas.HasValue || !m_AvailableAtlases.Contains(m_SelectedAtlas.Value)))
                m_SelectedAtlas = m_LoadedAtlases[0];

            m_Atlas.SetValueWithoutNotify(m_SelectedAtlas.HasValue ? Math.Max(0, Array.IndexOf(m_LoadedAtlases, m_SelectedAtlas.Value)) : 0);
            bool difumo = m_LoadedAtlases.Length > 0 && m_SelectedAtlas == TriangleEraser.AtlasSource.DiFuMo;
            m_DiFuMoVersion.gameObject.SetActive(difumo);
            m_DiFuMoThreshold.gameObject.SetActive(difumo);
            SetNumberText(m_DiFuMoThreshold, eraser.DiFuMoThreshold);
            if (difumo)
            {
                var versions = eraser.GetLoadedDiFuMoVersions();
                if (!versions.Contains(m_SelectedVersion))
                {
                    if (m_SelectedVersion != null) CloseRegions();
                    string displayedVersion = SelectedScene.FMRIManager.SelectedDiFuMoAtlas;
                    m_SelectedVersion = versions.Contains(displayedVersion) ? displayedVersion : versions.FirstOrDefault();
                }

                if (!versions.SequenceEqual(m_LoadedVersions))
                {
                    m_VersionPopup?.Close();
                    m_LoadedVersions = versions;
                    m_DiFuMoVersion.ClearOptions();
                    m_DiFuMoVersion.AddOptions(versions.ToList());
                }

                m_DiFuMoVersion.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(versions, m_SelectedVersion)));
            }

            bool ready = !eraser.IsComputingSelection && !eraser.HasTriangleStroke;
            m_Atlas.interactable = ready && m_LoadedAtlases.Length > 0;
            m_DiFuMoVersion.interactable = ready && difumo && m_LoadedVersions.Length > 0;
            m_RegionsButton.interactable = m_LoadedAtlases.Length > 0;
            if (!m_RegionsButton.interactable) CloseRegions();
            else if (m_Regions.Window.activeSelf) PopulateRegions(eraser);
        }

        private void PopulateRegions(TriangleEraser eraser)
        {
            if (m_SelectedAtlas is not { } source || !eraser.IsAtlasAvailable(source)) return;
            var surface = SelectedScene.MeshManager.ReferenceSurface;
            string version = source == TriangleEraser.AtlasSource.DiFuMo ? m_SelectedVersion : null;
            var context = (SelectedScene, surface, surface?.GeometryVersion ?? 0, source, version);
            if (m_ListContext != context)
            {
                m_ListContext = context;
                foreach (var row in m_RegionRows)
                {
                    row.Toggle.gameObject.SetActive(false);
                    Destroy(row.Toggle.gameObject);
                }

                m_RegionRows.Clear();
                m_Search.SetTextWithoutNotify(string.Empty);
                foreach (var region in eraser.GetAtlasRegions(source, version))
                {
                    Toggle toggle = Instantiate(m_RegionTogglePrefab, m_Scroll.content, false);
                    toggle.GetComponentInChildren<Text>().text = region.Name;
                    if (toggle.TryGetComponent(out Tooltip tooltip)) tooltip.Text = region.Name;
                    toggle.onValueChanged.AddListener(selected =>
                    {
                        if (!ListenerLock && HasCurrentContext && m_Regions.Window.activeInHierarchy &&
                            m_ListContext == context && m_SelectedAtlas == source &&
                            (source != TriangleEraser.AtlasSource.DiFuMo || m_SelectedVersion == version))
                            eraser.SetAtlasRegionSelected(source, version, region.Id, selected);
                    });
                    m_RegionRows.Add((toggle, region));
                }

                ScrollToTop();
            }

            foreach (var row in m_RegionRows)
            {
                row.Toggle.interactable = !eraser.IsComputingSelection;
                if (row.Toggle.interactable)
                    row.Toggle.SetIsOnWithoutNotify(eraser.IsAtlasRegionSelected(source, version, row.Region.Id));
            }
        }

        private void FilterRegions(string value)
        {
            string filter = value.Trim();
            foreach (var row in m_RegionRows)
                row.Toggle.gameObject.SetActive(row.Region.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0);
            ScrollToTop();
        }

        private void ScrollToTop()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Scroll.content);
            m_Scroll.StopMovement();
            m_Scroll.verticalNormalizedPosition = 1f;
        }

        private void CloseRegions()
        {
            if (m_Regions && m_Regions.Window && m_Regions.Window.activeSelf) m_Regions.Close();
        }
    }
}
