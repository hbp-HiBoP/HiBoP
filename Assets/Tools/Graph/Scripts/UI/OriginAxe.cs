using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class OriginAxe : MonoBehaviour
    {
        int width = 2;
        public enum TypeEnum { Abscissa , Ordinate}
        public TypeEnum Type { get; private set; }
        public void Set(TypeEnum type,Color color)
        {
            Type = type;
            GetComponent<Image>().color = color;
            RectTransform rectTransform = transform as RectTransform;
            if (type == TypeEnum.Abscissa)
            {
                gameObject.name = "Abscissa Origin";
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
            }
            else
            {
                gameObject.name = "Ordinate Origin";
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, width);
            }
        }
        public void UpdatePosition(Limits limits,Vector2 ratio)
        {
            RectTransform rectTransform = transform as RectTransform;
            Vector2 localPosition = Vector2.zero.GetLocalPosition(limits.Origin, ratio);
            switch (Type)
            {
                case TypeEnum.Abscissa:
                    rectTransform.offsetMax = new Vector2(localPosition.x, 0);
                    rectTransform.offsetMin = new Vector2(localPosition.x, 0);
                    rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
                        break;
                case TypeEnum.Ordinate:
                    rectTransform.offsetMax = new Vector2(0,localPosition.y);
                    rectTransform.offsetMin = new Vector2(0,localPosition.y);
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, width);
                    break;
            }
        }
    }
}