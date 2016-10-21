using UnityEngine;
using System.Collections;

public class ScreenWindowed : MonoBehaviour
{
	void Start ()
    {
        Screen.SetResolution(Screen.width, Screen.height, false);
	}

}
