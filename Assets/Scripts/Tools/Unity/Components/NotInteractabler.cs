using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Components
{
    [RequireComponent(typeof(Selectable))]
    public class NotInteractabler : MonoBehaviour
    {
        public void NotInteractable(bool interactable)
        {
            GetComponent<Selectable>().interactable = !interactable;
        }
    }
}