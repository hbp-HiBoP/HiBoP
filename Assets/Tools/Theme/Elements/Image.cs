﻿using UnityEngine;

namespace NewTheme
{
    [CreateAssetMenu()]
    public class Image : Element
    {
        public Sprite SourceImage;
        public Color Color;
        public Material Material;
        public UnityEngine.UI.Image.Type ImageType;
        public UnityEngine.UI.Image.FillMethod FillMethod;
        public bool PreserveAspect;
        public bool FillCenter;
        public bool Clockwise;
        public int FillOrigin;
        [Range(0.0f,1.0f)]
        public float FillAmount;

        public override void Set(GameObject gameObject)
        {
            UnityEngine.UI.Image image = gameObject.GetComponent<UnityEngine.UI.Image>();
            if(image)
            {
                image.sprite = SourceImage;
                image.color = Color;
                image.material = Material;
                image.type = ImageType;
                image.fillMethod = FillMethod;
                image.preserveAspect = PreserveAspect;
                image.fillCenter = FillCenter;
                image.fillAmount = FillAmount;
                image.fillClockwise = Clockwise;
                image.fillOrigin = FillOrigin;
            }
        }
    }
}