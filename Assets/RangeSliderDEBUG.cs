using System.Collections;
using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

public class RangeSliderDEBUG : MonoBehaviour
{
    public OldRangeSlider RangeSlider;

    private void Awake()
    {
        RangeSlider.Range = new Vector2(-300, 300);
        RangeSlider.Value = new Vector2(-150, 150);
    }
}
