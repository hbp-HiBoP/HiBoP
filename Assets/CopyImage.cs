using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CopyImage : MonoBehaviour
{
    public Image image;

	void OnEnable ()
    {

    }

    private void Update()
    {
        GetComponent<Image>().sprite = image.sprite;
    }
}
