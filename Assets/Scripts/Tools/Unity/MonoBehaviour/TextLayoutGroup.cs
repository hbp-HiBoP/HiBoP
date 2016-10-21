using UnityEngine;

namespace Tools.Unity
{
    public class TextLayoutGroup : MonoBehaviour
    {
        public TextLayout[] textLayouts;

        // Update is called once per frame
        void Update()
        {
            bool l_display = true;
            foreach (TextLayout display in textLayouts)
            {
                if(display.Dif < 50)
                {
                    l_display = false;
                    break;
                }
            }
            foreach(TextLayout display in textLayouts)
            {
                display.DisplayLabel(l_display);
            }
        }
    }
}

