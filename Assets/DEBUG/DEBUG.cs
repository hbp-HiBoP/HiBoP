using UnityEngine;
using Tools.Unity.Components;
using System;

public class DEBUG : MonoBehaviour
{
    public float Value;
    public float TOTO
    {
        get { return Value; }
        set { Value = value; }
    }
    public Single titeite;
    public Parser Parser;

    private void Start()
    {
        Parser.Parse(Value);
    }
}