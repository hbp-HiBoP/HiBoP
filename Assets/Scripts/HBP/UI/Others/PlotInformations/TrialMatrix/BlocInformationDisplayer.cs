using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{  
    public class BlocInformationDisplayer : MonoBehaviour
    {
        d.Bloc data;

        int line;
        float value;
        float latency;

        Text lineDisplayer;
        Text valueDisplayer;
        Text latencyDisplayer;

        RectTransform parentRectTransform;
        RectTransform rectTransform;


        void Awake()
        {
            lineDisplayer = transform.GetChild(0).GetChild(1).GetComponent<Text>();
            valueDisplayer = transform.GetChild(1).GetChild(1).GetComponent<Text>();
            latencyDisplayer = transform.GetChild(2).GetChild(1).GetComponent<Text>();
            rectTransform = transform.GetComponent<RectTransform>();
            parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }

        public void Set(d.Bloc bloc)
        {
            data = bloc;
        }

        public void SetActive(bool isActive)
        {
            transform.gameObject.SetActive(isActive);
        }

        void Update()
        {
            UpdatePosition();
            UpdateValues();
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            lineDisplayer.text = line.ToString();
            valueDisplayer.text = value.ToString();
            latencyDisplayer.text = latency.ToString();
        }

        void UpdatePosition()
        {
            transform.position = Input.mousePosition;
            CheckSide();
        }

        void UpdateValues()
        {
            Vector2 mousePosition = Input.mousePosition - parentRectTransform.position;
            mousePosition = new Vector2(mousePosition.x / parentRectTransform.rect.width, mousePosition.y / parentRectTransform.rect.height);
            mousePosition = new Vector2(Mathf.Clamp01(mousePosition.x), Mathf.Clamp01(mousePosition.y));
            line = Mathf.FloorToInt(mousePosition.y * data.Lines.Length);
            int l_instant = Mathf.FloorToInt(mousePosition.x * data.Lines[0].DataWithCorrection.Length);
            latency = data.PBloc.DisplayInformations.Window.x + mousePosition.x * (data.PBloc.DisplayInformations.Window.y - data.PBloc.DisplayInformations.Window.x);
            if(line >= data.Lines.Length)
            {
                line = data.Lines.Length-1;
            }
            else if(line < 0)
            {
                line = 0;
            }
            if(l_instant >= data.Lines[line].DataWithCorrection.Length)
            {
                l_instant = data.Lines[line].DataWithCorrection.Length-1;
            }
            else if(l_instant < 0)
            {
                l_instant = 0;
            }
            value = data.Lines[line].DataWithCorrection[l_instant];
        }

        void CheckSide()
        {
            Vector3[] corners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            parentRectTransform.GetWorldCorners(parentCorners);

            float botPosition = rectTransform.position.y - rectTransform.rect.height;
            float rightPosition = rectTransform.position.x + rectTransform.rect.width;

            if (botPosition > parentCorners[0].y && rightPosition < parentCorners[2].x)
            {
                rectTransform.pivot = new Vector2(0, 1);
            }
            else
            {
                if (corners[2].x > parentCorners[2].x)
                {
                    rectTransform.pivot = new Vector2(1, rectTransform.pivot.y);
                }
                if (corners[0].y < parentCorners[0].y)
                {
                    rectTransform.pivot = new Vector2(rectTransform.pivot.x, 0);
                }
            }
        }
    }
}
