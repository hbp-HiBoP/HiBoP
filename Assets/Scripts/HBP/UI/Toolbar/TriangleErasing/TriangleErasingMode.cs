using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HBP.Core.DLL;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class TriangleErasingMode : Tool
    {
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

        private static readonly TriangleEraser.AtlasSource[] s_AtlasSources = (TriangleEraser.AtlasSource[])Enum.GetValues(typeof(TriangleEraser.AtlasSource));

        private TriangleEraser.AtlasSource[] m_LoadedAtlases = Array.Empty<TriangleEraser.AtlasSource>();
        private TriangleEraser.AtlasSource m_SelectedAtlas;
        private string[] m_LoadedVersions = Array.Empty<string>();
        private string m_SelectedVersion;
        private (Base3DScene Scene, Surface Surface, long Geometry, TriangleEraser.AtlasSource Atlas, string Version)? m_ListContext;
        private readonly List<(Toggle Toggle, TriangleEraser.AtlasRegion Region)> m_RegionRows = new();
        private Tooltip m_ClearTooltip;

        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener(value =>
            {
                if (ListenerLock || !SelectedScene) return;
                SelectedScene.TriangleEraser.Mode = (TriangleSelectionMode)value;
                UpdateStatus();
                UpdateInteractable();
            });
            m_Add.onValueChanged.AddListener(value =>
            {
                if (value && !ListenerLock && SelectedScene) SelectedScene.TriangleEraser.RemoveFromSelection = false;
            });
            m_Remove.onValueChanged.AddListener(value =>
            {
                if (value && !ListenerLock && SelectedScene) SelectedScene.TriangleEraser.RemoveFromSelection = true;
            });
            m_Angle.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.Degrees = Mathf.Clamp(Mathf.RoundToInt(number), 0, 180)));
            m_Activity.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.ActivityTolerance = Mathf.Clamp(number, 0f, 100f)));
            m_BrushRadius.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.BrushRadiusMillimeters = number));
            m_DiFuMoThreshold.onEndEdit.AddListener(value => SetNumber(value, (eraser, number) => eraser.DiFuMoThreshold = Mathf.Clamp01(number)));
            m_EraseSelection.onClick.AddListener(() => Apply(false));
            m_KeepSelection.onClick.AddListener(() => Apply(true));
            m_RestoreSelection.onClick.AddListener(() =>
            {
                if (!ListenerLock && SelectedScene) SelectedScene.TriangleEraser.RestoreSelection();
            });
            m_ClearTooltip = m_ClearSelection.GetComponent<Tooltip>();
            m_ClearSelection.onClick.AddListener(() =>
            {
                if (!ListenerLock && SelectedScene) SelectedScene.TriangleEraser.ClearSelection();
            });

            m_Atlas.onValueChanged.AddListener(value =>
            {
                if (ListenerLock || !SelectedScene || (uint)value >= (uint)m_LoadedAtlases.Length) return;
                m_SelectedAtlas = m_LoadedAtlases[value];
                m_ListContext = null;
                UpdateAtlasOptions(SelectedScene.TriangleEraser);
            });
            m_DiFuMoVersion.onValueChanged.AddListener(value =>
            {
                if (ListenerLock || !SelectedScene || (uint)value >= (uint)m_LoadedVersions.Length) return;
                m_SelectedVersion = m_LoadedVersions[value];
                m_ListContext = null;
                UpdateAtlasOptions(SelectedScene.TriangleEraser);
            });
            m_Regions.OnChangeWindowState.AddListener(open =>
            {
                if (open && SelectedScene) PopulateRegions(SelectedScene.TriangleEraser);
            });
            m_Search.onValueChanged.AddListener(FilterRegions);
        }

        private void SetNumber(string value, Action<TriangleEraser, float> update)
        {
            if (ListenerLock || !SelectedScene) return;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) && float.IsFinite(number))
                update(SelectedScene.TriangleEraser, number);
            UpdateStatus();
        }

        private void Apply(bool keep)
        {
            if (!ListenerLock && SelectedScene) SelectedScene.TriangleEraser.ApplySelection(keep);
        }

        private void OnDisable() => CloseRegions();

        public override void DefaultState()
        {
            m_Dropdown.interactable = false;
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
            m_Activity.interactable = ready;
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
            m_Dropdown.SetValueWithoutNotify((int)eraser.Mode);
            if (eraser.IsSweepMode)
            {
                Toggle enabled = eraser.RemoveFromSelection ? m_Remove : m_Add;
                Toggle disabled = eraser.RemoveFromSelection ? m_Add : m_Remove;
                enabled.SetIsOnWithoutNotify(true);
                disabled.SetIsOnWithoutNotify(false);
            }

            ShowOptions(eraser.Mode);
            SetNumberText(m_Angle, eraser.Degrees);
            SetNumberText(m_Activity, eraser.ActivityTolerance);
            if (!m_BrushRadius.isFocused)
                m_BrushRadius.SetTextWithoutNotify(eraser.BrushRadiusMillimeters.ToString("G", CultureInfo.InvariantCulture));
            if (eraser.Mode == TriangleSelectionMode.Atlas) UpdateAtlasOptions(eraser);
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
            }

            m_Atlas.gameObject.SetActive(atlas);
            m_Regions.gameObject.SetActive(atlas);
        }

        private static void SetNumberText(InputField field, float value)
        {
            if (!field.isFocused) field.SetTextWithoutNotify(value.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private void UpdateAtlasOptions(TriangleEraser eraser)
        {
            var loaded = s_AtlasSources.Where(eraser.IsAtlasAvailable).ToArray();
            if (Array.IndexOf(loaded, m_SelectedAtlas) < 0) m_SelectedAtlas = loaded.FirstOrDefault();
            if (!loaded.SequenceEqual(m_LoadedAtlases))
            {
                m_LoadedAtlases = loaded;
                m_Atlas.ClearOptions();
                m_Atlas.AddOptions(loaded.Length == 0 ? new List<string> { "No atlas loaded" } : loaded.Select(atlas => atlas.ToString()).ToList());
            }

            m_Atlas.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(loaded, m_SelectedAtlas)));
            bool difumo = loaded.Length > 0 && m_SelectedAtlas == TriangleEraser.AtlasSource.DiFuMo;
            m_DiFuMoVersion.gameObject.SetActive(difumo);
            m_DiFuMoThreshold.gameObject.SetActive(difumo);
            SetNumberText(m_DiFuMoThreshold, eraser.DiFuMoThreshold);
            if (difumo)
            {
                var versions = eraser.GetLoadedDiFuMoVersions();
                if (!versions.Contains(m_SelectedVersion))
                {
                    string displayedVersion = SelectedScene.FMRIManager.SelectedDiFuMoAtlas;
                    m_SelectedVersion = versions.Contains(displayedVersion) ? displayedVersion : versions.FirstOrDefault();
                }

                if (!versions.SequenceEqual(m_LoadedVersions))
                {
                    m_LoadedVersions = versions;
                    m_DiFuMoVersion.ClearOptions();
                    m_DiFuMoVersion.AddOptions(versions.ToList());
                }

                m_DiFuMoVersion.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(versions, m_SelectedVersion)));
            }

            bool ready = !eraser.IsComputingSelection;
            m_Atlas.interactable = ready && loaded.Length > 0;
            m_DiFuMoVersion.interactable = ready && difumo;
            m_RegionsButton.interactable = loaded.Length > 0;
            if (!m_RegionsButton.interactable) CloseRegions();
            else if (m_Regions.Window.activeSelf) PopulateRegions(eraser);
        }

        private void PopulateRegions(TriangleEraser eraser)
        {
            var surface = SelectedScene.MeshManager.ReferenceSurface;
            string version = m_SelectedAtlas == TriangleEraser.AtlasSource.DiFuMo ? m_SelectedVersion : null;
            var context = (SelectedScene, surface, surface?.GeometryVersion ?? 0, m_SelectedAtlas, version);
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
                var source = m_SelectedAtlas;
                foreach (var region in eraser.GetAtlasRegions(source, version))
                {
                    Toggle toggle = Instantiate(m_RegionTogglePrefab, m_Scroll.content, false);
                    toggle.GetComponentInChildren<Text>().text = region.Name;
                    if (toggle.TryGetComponent(out Tooltip tooltip)) tooltip.Text = region.Name;
                    toggle.onValueChanged.AddListener(selected =>
                    {
                        if (!ListenerLock && SelectedScene && SelectedScene.TriangleEraser == eraser)
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
                    row.Toggle.SetIsOnWithoutNotify(eraser.IsAtlasRegionSelected(m_SelectedAtlas, version, row.Region.Id));
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
