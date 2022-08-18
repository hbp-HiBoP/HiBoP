using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Informations.TrialMatrix
{
    public class MainEventIndicator : MonoBehaviour
    {
        Text text;
        Image image;

        public void Set(Color color, string label)
        {
            if (!image) image = GetComponent<Image>();
            if (!text) text = GetComponentInChildren<Text>();
            image.color = color;
            text.text = label;
        }
    }
}