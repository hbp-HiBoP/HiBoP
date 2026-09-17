using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Serialized world-space Quest status card.</summary>
    public sealed class QuestStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text heading;
        [SerializeField] private Text code;
        [SerializeField] private Text address;
        [SerializeField] private Text message;

        public void ShowPairing(string value, string ip, bool locked)
        {
            gameObject.SetActive(true);
            heading.text = "Pair this Quest in HiBoP Desktop";
            code.text = locked ? "Code expired" : value;
            address.text = string.IsNullOrEmpty(ip) ? "" : "IP address: " + ip;
            message.text = locked ? "Press Y to create a new code." : "Select Quest > Pair a Quest on Desktop.";
        }

        public void ShowWaiting()
        {
            gameObject.SetActive(true);
            heading.text = "Quest paired";
            code.text = "";
            address.text = "";
            message.text = "Waiting for visualization";
        }

        public void ShowUnavailable(string details)
        {
            gameObject.SetActive(true);
            heading.text = "Quest connection unavailable";
            code.text = "";
            address.text = "";
            message.text = details;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
