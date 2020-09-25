using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Components
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