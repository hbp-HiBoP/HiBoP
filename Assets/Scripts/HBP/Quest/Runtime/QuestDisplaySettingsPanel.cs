using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync.Scene;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Quest presentation of the selected scene's shared display settings.</summary>
    public sealed class QuestDisplaySettingsPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestWindow window;
        [SerializeField] private Dropdown mesh, mri, implantation, colormap, brainColor, mriColor;
        [SerializeField] private Toggle inflated, edges, transparent, showAllSites, hideBlacklisted, automaticCuts;
        [SerializeField] private Slider opacity, siteSize;
        [SerializeField] private Text siteSizeValue, status;
        [SerializeField] private Button projection, removeProjection;
        [SerializeField] private Toggle marsAtlas, juBrain, ibc, difumo;
        [SerializeField] private Dropdown ibcContrast, difumoVersion, difumoContrast;
        [SerializeField] private GameObject ibcParameters, difumoParameters, localizerNotice, statusMessage;

        private static readonly ColorType[] Colormaps = { ColorType.Grayscale, ColorType.Hot, ColorType.Winter, ColorType.Warm, ColorType.Surface, ColorType.Cool, ColorType.RedYellow, ColorType.BlueGreen, ColorType.ACTC, ColorType.Bone, ColorType.GEColor, ColorType.Gold, ColorType.XRain, ColorType.MatLab };
        private static readonly ColorType[] BrainColors = { ColorType.BrainColor, ColorType.Default, ColorType.White, ColorType.Grayscale, ColorType.SoftGrayscale };
        private static readonly ColorType[] MRIColors = { ColorType.Default, ColorType.Grayscale };
        private Base3DScene scene;
        private Dropdown[] dropdowns;
        private Selectable[] controls;
        private string contrastVersion;
        private bool hadIBC, hadDiFuMo, changingRepresentation;
        private int meshCount = -1, mriCount = -1, implantationCount = -1;
        private int difumoCount = -1;

        public bool Ready => view != null && QuestAnatomySession.CanInteractWith(view.Scene) && view.Scene.IsVisible && view.Scene.MeshManager.Meshes.Count > 0 && view.Find(view.Scene) is { IsPlaced: true, IsTransitioning: false } && !view.Scene.IsActivityProjectionBusy && !view.Scene.IsSurfaceRepresentationPreparing && !view.Scene.IsSurfaceRepresentationTransitioning && !changingRepresentation && SceneReconciliationRecord.Get(view.Scene)?.Status is not (ReconciliationStatus.Reconciling or ReconciliationStatus.OutOfSync);

        private void Awake()
        {
            dropdowns = new[] { mesh, mri, implantation, colormap, brainColor, mriColor, ibcContrast, difumoVersion, difumoContrast };
            controls = dropdowns.Cast<Selectable>().Concat(new Selectable[] { inflated, edges, transparent, showAllSites, hideBlacklisted, automaticCuts, opacity, siteSize, projection, removeProjection, marsAtlas, juBrain, ibc, difumo }).ToArray();
            mesh.onValueChanged.AddListener(index => Change(target => target.MeshManager.Select(mesh.options[index].text)));
            mri.onValueChanged.AddListener(index => Change(target => target.MRIManager.Select(mri.options[index].text)));
            implantation.onValueChanged.AddListener(index => Change(target => target.ImplantationManager.Select(implantation.options[index].text)));
            colormap.onValueChanged.AddListener(index => Change(target => target.Colormap = Colormaps[index]));
            brainColor.onValueChanged.AddListener(index => Change(target => target.BrainColor = BrainColors[index]));
            mriColor.onValueChanged.AddListener(index => Change(target => target.CutColor = MRIColors[index]));
            edges.onValueChanged.AddListener(value => Change(target => target.EdgeMode = value));
            transparent.onValueChanged.AddListener(value => Change(target => target.IsBrainTransparent = value));
            opacity.onValueChanged.AddListener(value => Change(target => target.BrainMaterials.SetAlpha(value)));
            showAllSites.onValueChanged.AddListener(value => Change(target => target.ShowAllSites = value));
            hideBlacklisted.onValueChanged.AddListener(value => Change(target => target.HideBlacklistedSites = value));
            siteSize.onValueChanged.AddListener(value => Change(target => target.SiteGain = value));
            automaticCuts.onValueChanged.AddListener(value => Change(target => target.AutomaticCutAroundSelectedSite = value));
            projection.onClick.AddListener(() =>
            {
                if (Ready) view.RecalculateProjection();
            });
            removeProjection.onClick.AddListener(() => Change(target => target.SetProjectionEnabled(false)));
            inflated.onValueChanged.AddListener(value => ChangeRepresentationAsync(value).Forget());
            marsAtlas.onValueChanged.AddListener(value => SelectAtlas(0, value));
            juBrain.onValueChanged.AddListener(value => SelectAtlas(1, value));
            ibc.onValueChanged.AddListener(value => SelectAtlas(2, value));
            difumo.onValueChanged.AddListener(value => SelectAtlas(3, value));
            ibcContrast.onValueChanged.AddListener(index => Change(target => target.FMRIManager.SelectedIBCContrastID = index));
            difumoVersion.onValueChanged.AddListener(index => Change(target => target.FMRIManager.SelectedDiFuMoAtlas = difumoVersion.options[index].text));
            difumoContrast.onValueChanged.AddListener(index => Change(target => target.FMRIManager.SelectedDiFuMoArea = index));
            window.OnChanged.AddListener(OnWindowChanged);
            Refresh();
        }

        private void Update()
        {
            if (window.IsOpen || scene != (view != null ? view.Scene : null)) Refresh();
        }

        private void OnWindowChanged(QuestWindow value)
        {
            if (!value.IsOpen) CloseDropdowns();
        }

        private void CloseDropdowns()
        {
            foreach (var dropdown in dropdowns) dropdown.Hide();
        }

        private void Change(Action<Base3DScene> apply)
        {
            if (!Ready) return;
            // The popup may have been opened before a remote context change.
            if (scene != view.Scene)
            {
                Refresh();
                return;
            }

            try
            {
                apply(scene);
                status.text = "";
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                status.text = exception.Message;
            }

            Refresh();
        }

        private void SelectAtlas(int atlas, bool enabled)
        {
            Change(target =>
            {
                var fmri = target.FMRIManager;
                if (enabled)
                {
                    // Validate before disabling the current overlay, so a rejected choice preserves it.
                    target.EnsureAtlasCanBeUsed(atlas switch { 0 => "mars", 1 => "jubrain", 2 => "ibc", _ => "difumo:" + fmri.SelectedDiFuMoAtlas });
                }

                if (atlas == 0 || enabled && target.AtlasManager.DisplayMarsAtlas) target.AtlasManager.DisplayMarsAtlas = enabled && atlas == 0;
                if (atlas == 1 || enabled && target.AtlasManager.DisplayJuBrainAtlas) target.AtlasManager.DisplayJuBrainAtlas = enabled && atlas == 1;
                if (atlas == 2 || enabled && fmri.DisplayIBCContrasts) fmri.DisplayIBCContrasts = enabled && atlas == 2;
                if (atlas == 3 || enabled && fmri.DisplayDiFuMo) fmri.DisplayDiFuMo = enabled && atlas == 3;
                if (enabled && fmri.DisplayLocalizers) fmri.DisplayLocalizers = false;
            });
        }

        private async UniTaskVoid ChangeRepresentationAsync(bool value)
        {
            if (!Ready || scene != view.Scene)
            {
                Refresh();
                return;
            }

            var target = scene;
            changingRepresentation = true;
            Refresh();
            try
            {
                await SurfaceInflationLoading.ChangeRepresentationAsync(target, value ? SurfaceRepresentation.Inflated : SurfaceRepresentation.Anatomical, this.GetCancellationTokenOnDestroy());
            }
            finally
            {
                changingRepresentation = false;
                if (this != null) Refresh();
            }
        }

        public void Refresh()
        {
            var selected = view != null ? view.Scene : null;
            if (scene != selected)
            {
                CloseDropdowns();
                if (scene != null)
                {
                    scene.OnAddCut.RemoveListener(OnCutAdded);
                    scene.OnSurfaceRepresentationChanged.RemoveListener(OnRepresentationChanged);
                }

                scene = selected;
                if (scene != null)
                {
                    scene.OnAddCut.AddListener(OnCutAdded);
                    scene.OnSurfaceRepresentationChanged.AddListener(OnRepresentationChanged);
                }

                meshCount = mriCount = implantationCount = -1;
                contrastVersion = null;
                difumoCount = -1;
                hadIBC = hadDiFuMo = false;
                status.text = "";
            }

            bool ready = Ready;
            foreach (var control in controls) control.interactable = ready;
            statusMessage.SetActive(!string.IsNullOrEmpty(status.text));
            if (scene == null || scene.IsClosing || scene.MeshManager.Meshes.Count == 0)
            {
                ibcParameters.SetActive(false);
                difumoParameters.SetActive(false);
                localizerNotice.SetActive(false);
                return;
            }

            RefreshResources();
            var selectedMesh = scene.MeshManager.SelectedMesh;
            removeProjection.interactable = ready && scene.ProjectionRequested;
            mesh.SetValueWithoutNotify(scene.MeshManager.SelectedMeshID);
            mri.SetValueWithoutNotify(scene.MRIManager.SelectedMRIID);
            implantation.SetValueWithoutNotify(scene.ImplantationManager.SelectedImplantationID);
            mri.interactable = ready && mri.options.Count > 0;
            implantation.interactable = ready && implantation.options.Count > 0;
            colormap.SetValueWithoutNotify(Array.IndexOf(Colormaps, scene.Colormap));
            brainColor.SetValueWithoutNotify(Array.IndexOf(BrainColors, scene.BrainColor));
            mriColor.SetValueWithoutNotify(Array.IndexOf(MRIColors, scene.CutColor));
            inflated.SetIsOnWithoutNotify(selectedMesh.Representation == SurfaceRepresentation.Inflated);
            inflated.interactable = ready && (inflated.isOn || selectedMesh.TryGetInflationAvailability(out _));
            edges.SetIsOnWithoutNotify(scene.EdgeMode);
            transparent.SetIsOnWithoutNotify(scene.IsBrainTransparent);
            opacity.SetValueWithoutNotify(scene.BrainMaterials.Alpha);
            opacity.interactable = ready && scene.IsBrainTransparent;
            showAllSites.SetIsOnWithoutNotify(scene.ShowAllSites);
            hideBlacklisted.SetIsOnWithoutNotify(scene.HideBlacklistedSites);
            automaticCuts.SetIsOnWithoutNotify(scene.AutomaticCutAroundSelectedSite);
            siteSize.SetValueWithoutNotify(scene.SiteGain);
            siteSizeValue.text = scene.SiteGain.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "×";

            var fmri = scene.FMRIManager;
            marsAtlas.SetIsOnWithoutNotify(scene.AtlasManager.DisplayMarsAtlas);
            juBrain.SetIsOnWithoutNotify(scene.AtlasManager.DisplayJuBrainAtlas);
            ibc.SetIsOnWithoutNotify(fmri.DisplayIBCContrasts);
            difumo.SetIsOnWithoutNotify(fmri.DisplayDiFuMo);
            marsAtlas.interactable = ready && Object3DManager.MarsAtlas.Loaded && selectedMesh.SupportsMarsAtlas;
            juBrain.interactable = ready && Object3DManager.JuBrain.Loaded && selectedMesh.SupportsMNIResources;
            ibc.interactable = ready && Object3DManager.IBC.Loaded && selectedMesh.SupportsMNIResources;
            difumo.interactable = ready && Object3DManager.DiFuMo.Loaded && selectedMesh.SupportsMNIResources;
            ibcParameters.SetActive(fmri.DisplayIBCContrasts);
            difumoParameters.SetActive(fmri.DisplayDiFuMo);
            localizerNotice.SetActive(fmri.DisplayLocalizers);
            RefreshAtlasOptions();
            ibcContrast.SetValueWithoutNotify(fmri.SelectedIBCContrastID);
            ibcContrast.interactable = ready && fmri.DisplayIBCContrasts && ibcContrast.options.Count > 0;
            difumoVersion.interactable = ready && fmri.DisplayDiFuMo && difumoVersion.options.Count > 0;
            difumoContrast.interactable = ready && fmri.DisplayDiFuMo && difumoContrast.options.Count > 0;
            difumoContrast.SetValueWithoutNotify(fmri.SelectedDiFuMoArea);
        }

        private void RefreshResources()
        {
            if (meshCount != scene.MeshManager.Meshes.Count)
            {
                SetOptions(mesh, scene.MeshManager.Meshes.Select(value => value.Name.ToString()));
                meshCount = scene.MeshManager.Meshes.Count;
            }

            if (mriCount != scene.MRIManager.MRIs.Count)
            {
                SetOptions(mri, scene.MRIManager.MRIs.Select(value => value.Name));
                mriCount = scene.MRIManager.MRIs.Count;
            }

            if (implantationCount != scene.ImplantationManager.Implantations.Count)
            {
                SetOptions(implantation, scene.ImplantationManager.Implantations.Select(value => value.Name));
                implantationCount = scene.ImplantationManager.Implantations.Count;
            }
        }

        private void RefreshAtlasOptions()
        {
            if (hadIBC != Object3DManager.IBC.Loaded)
            {
                SetOptions(ibcContrast, Object3DManager.IBC.Loaded ? Object3DManager.IBC.Information.AllLabels.Select(value => value.PrettyName) : Enumerable.Empty<string>());
                hadIBC = Object3DManager.IBC.Loaded;
            }

            if (hadDiFuMo != Object3DManager.DiFuMo.Loaded || difumoCount != Object3DManager.DiFuMo.FMRIs.Count)
            {
                SetOptions(difumoVersion, Object3DManager.DiFuMo.Loaded ? Object3DManager.DiFuMo.FMRIs.Keys.OrderBy(int.Parse) : Enumerable.Empty<string>());
                hadDiFuMo = Object3DManager.DiFuMo.Loaded;
                difumoCount = Object3DManager.DiFuMo.FMRIs.Count;
                contrastVersion = null;
            }

            if (!hadDiFuMo) return;
            string version = scene.FMRIManager.SelectedDiFuMoAtlas;
            if (contrastVersion != version)
            {
                SetOptions(difumoContrast, Object3DManager.DiFuMo.Information.TryGetValue(version, out var information) ? information.AllLabels.Select(value => value.Name) : Enumerable.Empty<string>());
                contrastVersion = version;
            }

            difumoVersion.SetValueWithoutNotify(difumoVersion.options.FindIndex(value => value.text == version));
        }

        private static void SetOptions(Dropdown dropdown, IEnumerable<string> values)
        {
            dropdown.Hide();
            dropdown.ClearOptions();
            dropdown.AddOptions(values.ToList());
        }

        private void OnCutAdded(Cut cut) => SurfaceInflationLoading.ShowCutsInformationIfNeeded(scene);
        private void OnRepresentationChanged(SurfaceRepresentation representation) => SurfaceInflationLoading.ShowCutsInformationIfNeeded(scene);

        private void OnDestroy()
        {
            if (window != null) window.OnChanged.RemoveListener(OnWindowChanged);
            if (scene != null)
            {
                scene.OnAddCut.RemoveListener(OnCutAdded);
                scene.OnSurfaceRepresentationChanged.RemoveListener(OnRepresentationChanged);
            }
        }
    }
}
