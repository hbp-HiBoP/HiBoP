using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using UnityEngine;
using UnityEngine.UI;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Quest
{
    /// <summary>Offline MNI experiment runner. The normal Quest input/presentation pipeline performs every selection.</summary>
    public sealed class QuestSelectionLab : MonoBehaviour
    {
        public const int ExperimentRevision = 5;
        [SerializeField] private QuestAnatomyView anatomy;
        [SerializeField] private QuestSiteProbe probe;
        [SerializeField] private Transform head;
        [SerializeField] private Text status;
        [SerializeField] private QuestNumericKeypad keypad;
        [SerializeField] private Transform targetMarker;
        private QuestAnatomyView.Presentation presentation;
        private QuestSelectionSettings settings = QuestSelectionSettings.Default(), a, b;
        private QuestSelectionLayout layout;
        private int profile = 5, trialIndex;
        private bool busy = true, testing, slotB, targetVisible = true;
        private double started, frameSum;
        private int frameCount, transitions;
        private int ease = 3, confidence = 3, effort = 3;
        private double firstTargetSeconds = -1;
        private long stabilizedFramesAtStart;
        private Site target;
        private string message = "Chargement du MNI…", logDirectory, session;
        private Vector3 initialPosition, initialScale;
        private Quaternion initialRotation;
        private string[] sequence;
        private int settingsPage;
        private static readonly Vector3[] cursorPlacements = { Vector3.zero, new Vector3(0, .03f, .02f), new Vector3(0, 0, .06f) };
        private static readonly string[] cursorNames = { "Sphère bleue", "Halo contrasté", "Cage 3D", "Stylet court" };
        private static readonly string[] cursorColors = { "Blanc", "Ambre", "Cyan" };
        private QuestSelectionLabControl[] controls;
        public QuestSelectionSettings Settings => settings;
        public bool Ready => !busy && presentation != null && presentation.Scene != null && !presentation.Scene.IsClosing;
        public string LogDirectory => logDirectory;

        [Serializable]
        private sealed class Saved
        {
            public int Version = 1;
            public QuestSelectionSettings Current, A, B;
        }

        [Serializable]
        private sealed class Record
        {
            public string Event, Session, Utc, Build, Target, Selected, Outcome, Layout, Profile;
            public QuestSelectionSettings Settings;
            public double Seconds, MeanFrameMs;
            public double FirstTargetSeconds;
            public int Transitions;
            public int Ease, Confidence, Effort;
            public long StabilizedFrames;
            public bool TargetCue;
            public int RandomSeed;
            public Vector3 BrainPosition, BrainScale, HeadPosition;
            public Quaternion BrainRotation;
        }

        private void Awake()
        {
            // The standalone Editor has no headset compositor. Keep its expected absence out of experiment errors.
            if (Application.isEditor && TryGetComponent<QuestBootstrap>(out var bootstrap)) bootstrap.enabled = false;
            if (targetMarker != null)
            {
                var color = new MaterialPropertyBlock();
                color.SetColor("_BaseColor", Color.green);
                color.SetColor("_Color", Color.green);
                targetMarker.GetComponent<Renderer>().SetPropertyBlock(color);
            }
        }

        private async void Start()
        {
            try
            {
                await OpenAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception error)
            {
                busy = true;
                message = "Chargement impossible : " + error.Message;
                Debug.LogException(error);
            }
        }

        private async UniTask OpenAsync()
        {
            session = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            logDirectory = Path.Combine(Application.persistentDataPath, "selection-lab");
            Directory.CreateDirectory(logDirectory);
            LoadSettings();
            controls = GetComponentsInChildren<QuestSelectionLabControl>(true);
            RefreshPage();
            await StandardData.EnsureInstalledAsync();
            await HBP.Data.Module3D.Base3DScene.PrepareStandardResourcesAsync();
            await UniTask.SwitchToMainThread();
            var token = this.GetCancellationTokenOnDestroy();
            token.ThrowIfCancellationRequested();
            using var bounds = HBP.Core.DLL.BBox.Merge(Object3DManager.MNI.MRI.Volume.BoundingBox, Object3DManager.MNI.GreyMatter.Both.BoundingBox);
            Vector3 normal = Object3DManager.MNI.MRI.Volume.GetOrientationVector(CutOrientation.Axial, false).normalized;
            using var brainBounds = Object3DManager.MNI.GreyMatter.Both.BoundingBox;
            Vector3 brainMin = brainBounds.Min, brainMax = brainBounds.Max;
            var patient = QuestSelectionLayouts.Patient(bounds.Center, normal, new Bounds((brainMin + brainMax) * .5f, brainMax - brainMin));
            var configuration = new VisualizationConfiguration
            {
                MeshName = "MNI", MRIName = "MNI", ImplantationName = "MNI", ShowAllSites = true, StrongCuts = false, SiteGain = 1,
                Cuts = new() { new HBP.Core.Data.Cut("lab-axial", normal, CutOrientation.Axial, false, .5f) }
            };
            var visualization = new Visualization("Sélection — MNI", new[] { patient }, new Column[] { new AnatomicColumn("Sites expérimentaux", new BaseConfiguration { ActivityAlpha = 0 }, new AnatomicConfiguration(), "lab-column") }, configuration, "selection-lab-visualization");
            presentation = await anatomy.OpenLocalAsync(visualization, token, projectActivity: false);
            await UniTask.SwitchToMainThread();
            // Wait for a real tracked head before placing the anatomy in the standalone player.
            var tracker = head.GetComponent<QuestDevicePoseTracker>();
            if (!Application.isEditor && tracker != null) await UniTask.WaitUntil(() => tracker.IsTracked, cancellationToken: token);
            ResetPose();
            var wrapper = presentation.Columns[0].transform;
            Vector3 center = presentation.Columns[0].Manipulator.GrabCenter;
            wrapper.localScale *= 1.5f;
            wrapper.position += center - presentation.Columns[0].Manipulator.GrabCenter;
            initialPosition = wrapper.position;
            initialRotation = wrapper.rotation;
            initialScale = wrapper.localScale;
            // Align the depth-pair along the actual initial viewing direction, while keeping it in the removed half.
            var d1 = Sites.Single(s => s.Information.Name == "D1");
            var d2 = Sites.Single(s => s.Information.Name == "D2");
            Vector3 middle = (d1.transform.position + d2.transform.position) * .5f;
            Vector3 axis = (middle - head.position).normalized;
            // Positions are fixed for the whole session, including A/B comparisons.
            SetSitePosition(d1, middle - axis * .009f);
            SetSitePosition(d2, middle + axis * .009f);
            probe.ExperimentSettings = settings;
            probe.OnSelected.AddListener(OnSelected);
            busy = false;
            ApplyLayout();
            message = "Exploration libre. Maintenir A à droite pour sélectionner. Gâchettes : déplacer / agrandir le cerveau.";
            Log("session-start", "");
        }

        private Site[] Sites => presentation.Scene.Columns[0].Sites.ToArray();

        private static void SetSitePosition(Site site, Vector3 world)
        {
            site.transform.position = world;
            site.Information.DefaultPosition = site.transform.localPosition;
        }

        private void ResetPose()
        {
            if (presentation == null || presentation.Scene == null || presentation.Scene.IsClosing) return;
            probe.Cancel();
            anatomy.Place(presentation, new Pose(head.position, head.rotation), Camera.main, Array.Empty<Rect>());
        }

        private void RestorePose()
        {
            if (!Ready) return;
            Finish("pose-reset");
            probe.Cancel();
            var wrapper = presentation.Columns[0].transform;
            wrapper.SetPositionAndRotation(initialPosition, initialRotation);
            wrapper.localScale = initialScale;
            wrapper.GetComponent<QuestAnatomyManipulator>().CancelGrab();
            presentation.Scene.Columns[0].UnselectSite();
        }

        private void ApplyLayout()
        {
            probe.Cancel();
            target = null;
            foreach (var site in Sites) site.State.IsMasked = !site.Information.Name.StartsWith(QuestSelectionLayouts.Prefix(layout), StringComparison.Ordinal);
            // IsMasked is a prepared-data flag, without a presentation-change event.
            // Request the common rendering update so visibility and selectable contacts agree.
            presentation.Scene.SceneInformation.SitesNeedUpdate = true;
            presentation.Scene.Columns[0].UnselectSite();
            var candidates = Sites.Where(s => !s.State.IsMasked);
            if (layout == QuestSelectionLayout.RandomBox)
            {
                var cut = presentation.Scene.Cuts.Single();
                candidates = candidates.Where(s => Vector3.Dot(cut.Normal, s.transform.localPosition - cut.Point) > 0);
            }

            sequence = candidates.OrderBy(s => int.Parse(s.Information.Name.Substring(1), CultureInfo.InvariantCulture)).Select(s => s.Information.Name).ToArray();
            trialIndex = 0;
        }

        private void Update()
        {
            if (testing)
            {
                frameSum += Time.unscaledDeltaTime;
                frameCount++;
            }

            if (targetMarker != null)
            {
                targetMarker.gameObject.SetActive(Ready && targetVisible && target != null);
                if (target != null)
                {
                    targetMarker.position = target.transform.position;
                    targetMarker.rotation = head.rotation;
                    targetMarker.localScale = Vector3.one * .007f;
                }
            }

            if (status != null) status.text = !Ready ? message : $"Sites : {QuestSelectionLayouts.Names[(int)layout]}\nSélection : {ProfileLabel} · {ModeLabel}\n{QuestSelectionLayouts.Instructions[(int)layout]}\n" + $"Cible : {target?.Information.Name ?? "libre"} — site choisi : {presentation.Scene.SelectedColumn?.SelectedSite?.Information.Name ?? "aucune"}\n{message}" + (settings.Debug ? $"\nDiagnostic : {probe.Assistance.CandidateCount} candidat(s), {(probe.Assistance.Retained ? "stabilisation" : "choix courant")}" : "");
        }

        private string ProfileLabel => profile >= 0 ? QuestSelectionSettings.ProfileNames[profile] : "Personnalisé";
        private string ModeLabel => new[] { "Contact", "Profondeur", "Rayon" }[(int)settings.Mode];

        private void OnSelected(Site site)
        {
            if (testing) transitions++;
            if (testing && site == target && firstTargetSeconds < 0) firstTargetSeconds = Time.realtimeSinceStartupAsDouble - started;
            Log("selection", "");
            if (testing && layout == QuestSelectionLayout.Row && site == target)
            {
                if (++trialIndex % sequence.Length == 0)
                {
                    Finish("success");
                    message = "Parcours terminé. Refaire ou changer de profil.";
                }
                else
                {
                    target = Sites.Single(s => s.Information.Name == sequence[trialIndex % sequence.Length]);
                    firstTargetSeconds = -1;
                    Log("sequence-target", "");
                }
            }
        }

        private void Begin()
        {
            if (!Ready) return;
            Finish("restarted");
            if (layout == QuestSelectionLayout.Row) trialIndex = 0;
            probe.Cancel();
            presentation.Scene.Columns[0].UnselectSite();
            target = Sites.Single(s => s.Information.Name == sequence[trialIndex % sequence.Length]);
            testing = true;
            transitions = 0;
            frameCount = 0;
            frameSum = 0;
            firstTargetSeconds = -1;
            stabilizedFramesAtStart = probe.StabilizedFrameCount;
            started = Time.realtimeSinceStartupAsDouble;
            message = layout == QuestSelectionLayout.Row ? "Parcours automatique R1 → R8. Garder A appuyé ; chaque cible correcte fait avancer le repère." : "Essai en cours. Sélectionner la cible puis utiliser « Valider ». Le repère de cible peut être masqué.";
            Log("trial-start", "");
        }

        private void Finish(string outcome)
        {
            if (!testing) return;
            Log("trial-end", outcome);
            testing = false;
        }

        public void Execute(string command)
        {
            if (!Ready) return;
            switch (command)
            {
                case "trial":
                    Begin();
                    return;
                case "confirm":
                    if (layout == QuestSelectionLayout.Row)
                    {
                        message = "La rangée se valide automatiquement après R8. Utiliser « Commencer / refaire » pour repartir de R1.";
                        return;
                    }

                    if (!testing)
                    {
                        message = "Commencer un essai avant de valider.";
                        return;
                    }

                    bool success = presentation.Scene.SelectedColumn.SelectedSite == target;
                    Finish(success ? "success" : "wrong-target");
                    message = success ? "Cible correcte. Passer à la cible suivante." : "Autre site sélectionné. Refaire l'essai ou continuer.";
                    return;
                case "skip":
                    Finish("abandoned");
                    trialIndex++;
                    Begin();
                    return;
                case "next":
                    Finish("next-target");
                    trialIndex++;
                    Begin();
                    return;
                case "layout":
                    Finish("layout-changed");
                    layout = (QuestSelectionLayout)(((int)layout + 1) % QuestSelectionLayouts.Names.Length);
                    ApplyLayout();
                    message = layout == QuestSelectionLayout.RandomBox ? "2 000 sites affichés. Les cibles guidées sont dans la moitié dégagée. Commencer un essai ou explorer librement." : $"Disposition changée : {sequence.Length} site(s). Utiliser « Commencer / refaire » pour lancer un essai.";
                    Log("layout-changed", "");
                    return;
                case "page":
                    settingsPage = (settingsPage + 1) % 5;
                    RefreshPage();
                    return;
                case "ease":
                    ease = ease % 5 + 1;
                    Log("rating", "ease");
                    return;
                case "confidence":
                    confidence = confidence % 5 + 1;
                    Log("rating", "confidence");
                    return;
                case "effort":
                    effort = effort % 5 + 1;
                    Log("rating", "effort");
                    return;
                case "reset-pose":
                    RestorePose();
                    return;
                case "target":
                    targetVisible = !targetVisible;
                    Log("target-cue", targetVisible.ToString());
                    return;
                case "save-a":
                    a = settings.Copy();
                    slotB = false;
                    SaveSettings();
                    message = "Réglages de sélection A enregistrés. La disposition des sites reste indépendante.";
                    return;
                case "save-b":
                    b = settings.Copy();
                    slotB = true;
                    SaveSettings();
                    message = "Réglages de sélection B enregistrés. La disposition des sites reste indépendante.";
                    return;
                case "ab":
                    if (a == null || b == null)
                    {
                        message = "Enregistrer A et B avant de comparer.";
                        return;
                    }

                    slotB = !slotB;
                    Change((slotB ? b : a).Copy());
                    message = "Réglages de sélection " + (slotB ? "B" : "A") + " appliqués. Même disposition des sites.";
                    return;
                case "profile":
                    profile = (profile + 1) % QuestSelectionSettings.ProfileNames.Length;
                    var nextProfile = QuestSelectionSettings.Profile(profile);
                    nextProfile.CopyCursorFrom(settings);
                    Change(nextProfile, profile);
                    message = "Profil « " + ProfileLabel + " » appliqué. Même disposition des sites.";
                    return;
                case "cursor-style":
                    Change(settings.Copy(), profile);
                    settings.CursorStyle = (QuestCursorStyle)(((int)settings.CursorStyle + 1) % cursorNames.Length);
                    break;
                case "cursor-color":
                    Change(settings.Copy(), profile);
                    settings.CursorColor = (QuestCursorColor)(((int)settings.CursorColor + 1) % cursorColors.Length);
                    break;
                case "offset-preset":
                    Change(settings.Copy(), profile);
                    int placement = Array.FindIndex(cursorPlacements, p => p == settings.CursorOffset);
                    settings.CursorOffset = cursorPlacements[(placement + 1) % cursorPlacements.Length];
                    message = "Position du curseur et point de sélection déplacés ensemble. X : droite ; Y : haut ; Z : avant du rayon.";
                    break;
                case "offset-reset":
                    Change(settings.Copy(), profile);
                    settings.CursorOffset = Vector3.zero;
                    break;
                case "mode":
                    Change(settings.Copy());
                    settings.Mode = (QuestSelectionMode)(((int)settings.Mode + 1) % 3);
                    SaveSettings();
                    return;
                case "candidate":
                    Change(settings.Copy());
                    settings.CandidateCue = !settings.CandidateCue;
                    break;
                case "link":
                    Change(settings.Copy());
                    settings.LinkCue = !settings.LinkCue;
                    break;
                case "name":
                    Change(settings.Copy());
                    settings.NameCue = !settings.NameCue;
                    break;
                case "attraction":
                    Change(settings.Copy());
                    settings.Attraction = !settings.Attraction;
                    break;
                case "stabilize":
                    Change(settings.Copy());
                    settings.Stabilize = !settings.Stabilize;
                    break;
                case "haptics":
                    Change(settings.Copy());
                    settings.Haptics = !settings.Haptics;
                    break;
                case "debug":
                    Change(settings.Copy());
                    settings.Debug = !settings.Debug;
                    break;
                case "defaults":
                    Change(QuestSelectionSettings.Default(), 5);
                    return;
                default:
                    if (keypad != null)
                        keypad.OpenValidated(this, ControlLabel(command), Number(command).ToString("0.###", CultureInfo.InvariantCulture), text =>
                        {
                            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || !float.IsFinite(value) || value < 0 && !command.StartsWith("offset-", StringComparison.Ordinal)) return false;
                            Change(settings.Copy(), command.StartsWith("cursor-", StringComparison.Ordinal) || command.StartsWith("offset-", StringComparison.Ordinal) ? profile : -1);
                            SetNumber(command, value);
                            settings.Validate();
                            SaveSettings();
                            return true;
                        });
                    return;
            }

            SaveSettings();
        }

        private void Change(QuestSelectionSettings value, int preset = -1)
        {
            Finish("settings-changed");
            probe.Cancel();
            settings = value;
            settings.Validate();
            profile = preset;
            probe.ExperimentSettings = settings;
            SaveSettings();
        }

        private void RefreshPage()
        {
            foreach (var control in controls) control.gameObject.SetActive(control.Page < 0 || control.Page == settingsPage);
        }

        public bool ControlEnabled(string command) =>
            Ready && (command switch
            {
                "confirm" => testing && layout != QuestSelectionLayout.Row,
                "next" => layout != QuestSelectionLayout.Row,
                "skip" => testing,
                "depth" => settings.Mode == QuestSelectionMode.Depth,
                "angle" => settings.Mode == QuestSelectionMode.Ray && settings.Attraction,
                "range" => settings.Mode == QuestSelectionMode.Ray,
                "reach" => settings.Attraction,
                "margin" => settings.Stabilize,
                "amplitude" => settings.Haptics,
                "label-size" => settings.NameCue,
                "cursor-color" or "cursor-size" => settings.CursorStyle != QuestCursorStyle.Sphere,
                _ => true
            });

        private float Number(string c) => c switch { "radius" => settings.ContactRadius * 1000, "marker" => settings.MarkerRadius * 1000, "depth" => settings.DepthTolerance * 1000, "reach" => settings.AttractionRadius * 1000, "range" => settings.RayDistance, "angle" => settings.RayAngle, "margin" => settings.SwitchMargin * 100, "amplitude" => settings.HapticAmplitude * 100, "label-size" => settings.LabelScale * 100, "cursor-size" => settings.CursorGuideRadius * 1000, "offset-x" => settings.CursorOffset.x * 1000, "offset-y" => settings.CursorOffset.y * 1000, "offset-z" => settings.CursorOffset.z * 1000, _ => 0 };

        private void SetNumber(string c, float v)
        {
            switch (c)
            {
                case "radius": settings.ContactRadius = v / 1000; break;
                case "marker": settings.MarkerRadius = v / 1000; break;
                case "depth": settings.DepthTolerance = v / 1000; break;
                case "reach": settings.AttractionRadius = v / 1000; break;
                case "range": settings.RayDistance = v; break;
                case "angle": settings.RayAngle = v; break;
                case "margin": settings.SwitchMargin = v / 100; break;
                case "amplitude": settings.HapticAmplitude = v / 100; break;
                case "label-size": settings.LabelScale = v / 100; break;
                case "cursor-size": settings.CursorGuideRadius = v / 1000; break;
                case "offset-x": settings.CursorOffset.x = v / 1000; break;
                case "offset-y": settings.CursorOffset.y = v / 1000; break;
                case "offset-z": settings.CursorOffset.z = v / 1000; break;
            }
        }

        public string ControlLabel(string c)
        {
            string On(bool value) => value ? "oui" : "non";
            return c switch
            {
                "profile" => "Profil de sélection : " + ProfileLabel, "mode" => "Méthode : " + ModeLabel,
                "page" => "Réglages " + (settingsPage + 1) + "/5 — " + (settingsPage == 3 ? "Curseur" : settingsPage == 4 ? "Notes" : "Sélection") + " — page suivante",
                "cursor-style" => "Curseur : " + cursorNames[(int)settings.CursorStyle],
                "cursor-color" => "Couleur curseur : " + cursorColors[(int)settings.CursorColor],
                "cursor-size" => $"Rayon des anneaux : {Number(c):0.##} mm",
                "offset-x" => $"Décalage X droite : {Number(c):0.##} mm",
                "offset-y" => $"Décalage Y haut : {Number(c):0.##} mm",
                "offset-z" => $"Décalage Z avant : {Number(c):0.##} mm",
                "offset-preset" => "Position rapide : " + (settings.CursorOffset == new Vector3(0, 0, .03f) ? "base validée (+30Z)" : settings.CursorOffset == cursorPlacements[0] ? "origine" : settings.CursorOffset == cursorPlacements[1] ? "haute (+30Y, +20Z)" : settings.CursorOffset == cursorPlacements[2] ? "avant (+60Z)" : "personnalisée"),
                "offset-reset" => "Annuler les décalages du curseur",
                "ease" => $"Facilité : {ease}/5", "confidence" => $"Confiance : {confidence}/5", "effort" => $"Effort : {effort}/5",
                "label-size" => $"Taille label : {Number(c):0.#} %",
                "layout" => "Disposition : " + QuestSelectionLayouts.Names[(int)layout], "candidate" => "Contour candidat : " + On(settings.CandidateCue), "link" => "Liaison : " + On(settings.LinkCue), "name" => "Nom candidat : " + On(settings.NameCue),
                "attraction" => "Attraction : " + On(settings.Attraction), "stabilize" => "Stabilisation : " + On(settings.Stabilize), "haptics" => "Vibration : " + On(settings.Haptics), "debug" => "Diagnostic : " + On(settings.Debug),
                "radius" => $"Contact : {Number(c):0.##} mm", "marker" => $"Boule visible : {Number(c):0.##} mm", "depth" => $"Profondeur ± : {Number(c):0.##} mm", "reach" => $"Attraction : {Number(c):0.##} mm", "range" => $"Portée rayon : {Number(c):0.##} m", "angle" => $"Demi-angle : {Number(c):0.##}°", "margin" => $"Marge stabilité : {Number(c):0.##} %", "amplitude" => $"Vibration : {Number(c):0.##} %",
                "trial" => "Commencer / refaire", "confirm" => "Valider la sélection", "next" => "Cible suivante", "skip" => "Abandonner et continuer", "target" => "Repère cible : " + On(targetVisible), "reset-pose" => "Restaurer pose initiale", "save-a" => "Enregistrer sélection A", "save-b" => "Enregistrer sélection B", "ab" => "Comparer sélection A / B", "defaults" => "Réglages de base validés", _ => c
            };
        }

        private void SaveSettings()
        {
            try
            {
                File.WriteAllText(Path.Combine(logDirectory, "settings.json"), JsonUtility.ToJson(new Saved { Current = settings, A = a, B = b }, true));
            }
            catch (Exception e)
            {
                message = "Sauvegarde impossible : " + e.Message;
            }
        }

        private void LoadSettings()
        {
            try
            {
                string path = Path.Combine(logDirectory, "settings.json");
                if (!File.Exists(path)) return;
                var saved = JsonUtility.FromJson<Saved>(File.ReadAllText(path));
                if (saved?.Version != 1) return;
                settings = saved.Current ?? settings;
                settings.Validate();
                a = saved.A;
                b = saved.B;
                a?.Validate();
                b?.Validate();
                profile = JsonUtility.ToJson(settings) == JsonUtility.ToJson(QuestSelectionSettings.Default()) ? 5 : -1;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Selection lab settings: " + e.Message);
            }
        }

        private void Log(string kind, string outcome)
        {
            if (presentation == null || presentation.Scene == null || presentation.Scene.IsClosing) return;
            var wrapper = presentation.Columns[0].transform;
            var record = new Record
            {
                Event = kind, Session = session, Utc = DateTime.UtcNow.ToString("O"), Build = Application.version + "/selection-lab-" + ExperimentRevision, Target = target?.Information.FullID, Selected = presentation.Scene.SelectedColumn?.SelectedSite?.Information.FullID,
                Outcome = outcome, Layout = layout.ToString(), Profile = ProfileLabel, Settings = settings.Copy(), Seconds = testing ? Time.realtimeSinceStartupAsDouble - started : 0, Transitions = transitions,
                StabilizedFrames = testing ? probe.StabilizedFrameCount - stabilizedFramesAtStart : 0, TargetCue = targetVisible, RandomSeed = layout == QuestSelectionLayout.RandomBox ? QuestSelectionLayouts.RandomSeed : 0,
                FirstTargetSeconds = firstTargetSeconds, MeanFrameMs = frameCount > 0 ? frameSum * 1000 / frameCount : 0, Ease = ease, Confidence = confidence, Effort = effort, BrainPosition = wrapper.position, BrainRotation = wrapper.rotation, BrainScale = wrapper.localScale, HeadPosition = head.position
            };
            try
            {
                File.AppendAllText(Path.Combine(logDirectory, session + ".jsonl"), JsonUtility.ToJson(record) + "\n");
            }
            catch (Exception e)
            {
                message = "Journal impossible : " + e.Message;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Finish("paused");
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) Finish("focus-lost");
        }

        private void OnDestroy()
        {
            Finish("closed");
            if (probe != null)
            {
                probe.OnSelected.RemoveListener(OnSelected);
                probe.ExperimentSettings = null;
                probe.Cancel();
            }
        }
    }
}
