using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace HBP.Quest
{
    public sealed class QuestBootstrap : MonoBehaviour
    {
        [SerializeField] private QuestPassthroughStatus passthrough;
        [SerializeField] private QuestDevicePoseTracker head;
        [SerializeField] private QuestDevicePoseTracker leftController;
        [SerializeField] private QuestDevicePoseTracker rightController;
        [SerializeField] private TextMesh statusText;
        private float nextStatusUpdate;
        private float nextLog;
        private float startedAt;
        private string previousProblem;

        public void Configure(QuestPassthroughStatus provider, QuestDevicePoseTracker configuredHead, QuestDevicePoseTracker left, QuestDevicePoseTracker right, TextMesh text)
        {
            passthrough = provider;
            head = configuredHead;
            leftController = left;
            rightController = right;
            statusText = text;
        }

        private void OnEnable() => startedAt = Time.unscaledTime;

        private void OnApplicationPause(bool paused)
        {
            if (!paused) startedAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextStatusUpdate) return;
            nextStatusUpdate = Time.unscaledTime + 0.25f;
            string problem = passthrough == null ? "Missing passthrough reference" : passthrough.GetProblem();
            bool failed = problem != null && Time.unscaledTime - startedAt >= 10f;
            string state = problem == null ? "COMPOSITION READY" : failed ? "PASSTHROUGH FAULT" : "STARTING";
            if (statusText != null)
            {
                statusText.color = failed ? new Color(1f, 0.35f, 0.25f) : Color.white;
                statusText.text = $"HiBoP Quest | {state}\n" + (problem == null ? "Room visibility: confirm in headset\n" : problem + "\n") + $"Head: {State(head)}\nLeft (blue): {State(leftController)}\nRight (orange): {State(rightController)}";
            }

            if (failed && problem != previousProblem) Debug.LogError("QUEST-004 passthrough fault: " + problem, this);
            previousProblem = failed ? problem : null;
            if (Time.unscaledTime < nextLog) return;
            nextLog = Time.unscaledTime + 5f;
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            var display = loader?.GetLoadedSubsystem<XRDisplaySubsystem>();
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Debug.Log($"QUEST-004 state={state}; problem={problem ?? "none"}; loader={loader?.GetType().Name ?? "none"}; runtime={OpenXRRuntime.name}; display={display?.running}; graphics={SystemInfo.graphicsDeviceType}; pipeline={pipeline?.name}; HDR={pipeline?.supportsHDR}; MSAA={pipeline?.msaaSampleCount}; stereo={OpenXRSettings.Instance?.renderMode}; head={Pose(head)}; left={Pose(leftController)}; right={Pose(rightController)}", this);
        }

        private static string State(QuestDevicePoseTracker tracker) => tracker != null && tracker.IsTracked ? "TRACKED" : "not tracked";
        private static string Pose(QuestDevicePoseTracker tracker) => tracker == null ? "missing" : $"{State(tracker)} pos={tracker.transform.localPosition:F3} rot={tracker.transform.localRotation.eulerAngles:F1}";
    }
}
