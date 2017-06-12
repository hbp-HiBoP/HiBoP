using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortCutDebug : MonoBehaviour
{
	void Update ()
    {
        KeyCode myKeyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), "LEFTCONTROL");
        Debug.Log(myKeyCode == KeyCode.LeftControl);
	}
}
