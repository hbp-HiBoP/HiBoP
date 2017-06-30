using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Tab : MonoBehaviour
{
    public float Height;
    [HideInInspector]
    public UnityEvent OnOpen = new UnityEvent();
    [HideInInspector]
    public UnityEvent OnClose = new UnityEvent();
    public Graphic[] Graphics { get { return GetComponentsInChildren<Graphic>(true); } }
   
    public void SetActive(bool active)
    {
        if (active) OnOpen.Invoke();
        else OnClose.Invoke();
    }
}
