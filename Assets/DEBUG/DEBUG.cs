using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Components;
using System;

public class DEBUG : MonoBehaviour
{
    private void Update()
    {
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        Debug.Log("ignoreLayout: "+layoutElement.ignoreLayout);

        Debug.Log("minWidth: " + layoutElement.minWidth);
        Debug.Log("minHeight: " + layoutElement.minHeight);

        Debug.Log("preferredWidth: " + layoutElement.preferredWidth);
        Debug.Log("preferredHeight: " + layoutElement.preferredHeight);

        Debug.Log("flexibleWidth: " + layoutElement.flexibleWidth);
        Debug.Log("flexibleHeight: " + layoutElement.flexibleHeight);

    }
}