using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tools.CSharp;

public class MedianDebug : MonoBehaviour
{

	void Start ()
    {
        float[] myFloats = new float[] { 6.5f, 8.9f, 3.2f,20.14f, 1.24f, 45.204f, 14.12f, 24 };
        Debug.Log(myFloats.Median().ToString("0.0000"));
	}
}
