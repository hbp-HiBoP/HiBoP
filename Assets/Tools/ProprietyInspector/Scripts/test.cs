using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviourWithProp
{
    [SerializeField]
    private int myint;
    public int MonToto
    {
        get { return myint; }
        set { myint = value; }
    }
    [SerializeField]
    private float myFloat;
    public float MyFloat
    {
        get { return myFloat; }
        set { myFloat = value; }
    }

    [SerializeField]
    private byte myByte;
    public byte MyByte
    {
        get { return myByte; }
        set { myByte = value; }
    }

    [SerializeField]
    private bool mybool;
    public bool MyBool
    {
        get { return mybool; }
        set { mybool = value; }
    }

    [SerializeField]
    private RectTransform myRect;
    public RectTransform MyRect
    {
        get { return myRect; }
        set { myRect = value; }
    }

    [SerializeField]
    private char myChar;
    public char MyChar
    {
        get { return myChar; }
        set { myChar = value; }
    }



}
