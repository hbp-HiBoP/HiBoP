using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace CRNL.HiBoP.XR.Bootstrap.Meta
{
    public sealed class P04MetaDisplayConfigurator : MonoBehaviour
    {
        private const float BaselineRefreshRate = 72f;
        private const int DisplayStartupGraceFrames = 300;

        private IEnumerator Start()
        {
            for (int frame = 0; frame < DisplayStartupGraceFrames; frame++)
            {
                XRDisplaySubsystem display = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRDisplaySubsystem>();
                if (display != null && display.running)
                {
                    RequestBaseline(display);
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning("P04 could not query the display subsystem; the runtime refresh rate is retained.", this);
        }

        private void RequestBaseline(XRDisplaySubsystem display)
        {
            if (!display.TryGetSupportedDisplayRefreshRates(Allocator.Temp, out NativeArray<float> supportedRates))
            {
                Debug.LogWarning("P04 could not query supported refresh rates; the runtime rate is retained.", this);
                return;
            }

            using (supportedRates)
            {
                for (int index = 0; index < supportedRates.Length; index++)
                {
                    if (!Mathf.Approximately(supportedRates[index], BaselineRefreshRate))
                    {
                        continue;
                    }

                    bool requested = display.TryRequestDisplayRefreshRate(BaselineRefreshRate);
                    Debug.Log($"P04 72 Hz baseline request: {(requested ? "accepted" : "rejected")}.", this);
                    return;
                }
            }

            Debug.LogWarning("P04 runtime does not advertise 72 Hz; the runtime rate is retained.", this);
        }
    }
}
