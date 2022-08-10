﻿using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Display.UI.Tools
{
    public class BlockerButton : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            GetComponent<Button>().onClick.Invoke();
        }
    }
}