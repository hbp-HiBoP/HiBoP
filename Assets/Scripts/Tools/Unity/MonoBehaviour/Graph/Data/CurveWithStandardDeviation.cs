using UnityEngine;

namespace Tools.Unity.Graph.Data
{
    public class CurveWithStandardDeviation : Curve
    {
        #region Properties
        protected float[] m_standardDeviations;
        public float[] StandardDeviations { get { return m_standardDeviations; } set { m_standardDeviations = value;} }

        protected float m_standardDeviationTransparency;
        public float StandardDeviationTransparency { get { return m_standardDeviationTransparency; } set { m_standardDeviationTransparency = value; } }
        #endregion

        #region Constructor
        public CurveWithStandardDeviation(string label, float width, Color color, Vector2[] points, float[] standardDeviations, Point.Style style, bool connectingPoints) : base(label,width,color,points,style,connectingPoints)
        {
            StandardDeviations = standardDeviations;
        }

        public CurveWithStandardDeviation() : this("", 0, Color.black, new Vector2[0], new float[0], Point.Style.Round, true)
        {
        }

        #endregion

        #region Private Methods
        Texture2D GenerateTexture(Vector2 point1,float sd1,Vector2 point2,float sd2, out float pivot)
        {
            int width = 64;
            // Detect which point is TOP/BOT/LEFT/RIGHT side.
            Vector2 bl, br, tl, tr;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            if (point1.x > point2.x)
            {
                bl = new Vector3(point2.x, point2.y - sd2 / 2);
                tl = new Vector2(point2.x, point2.y + sd2 / 2);
                br = new Vector2(point1.x, point1.y - sd1 / 2);
                tr = new Vector2(point1.x, point1.y + sd1 / 2);
                xMin = point2.x;
                xMax = point1.x;
            }
            else
            {
                bl = new Vector2(point1.x, point1.y - sd1 / 2);
                tl = new Vector2(point1.x, point1.y + sd1 / 2);
                br = new Vector3(point2.x, point2.y - sd2 / 2);
                tr = new Vector2(point2.x, point2.y + sd2 / 2);
                xMin = point1.x;
                xMax = point2.x;
            }

            //Calculate Limits.
            float yMax = float.MinValue;
            if (tl.y > tr.y)
            {
                yMax = tl.y;
            }
            else
            {
                yMax = tr.y;
            }
            float yMin = float.MaxValue;
            if (bl.y > br.y)
            {
                yMin = br.y;
            }
            else
            {
                yMin = bl.y;
            }
            // Calculate X,Y range.        
            float xDif = xMax - xMin;
            float yDif = yMax - yMin;
            pivot = (point1.y - yMin) / yDif;

            // Calculate Width/Height of the texture.
            int height = Mathf.RoundToInt(width * (yDif / xDif));

            // Calculate X ration and Y ratio.
            float xR = xDif / width;
            float yR = yDif / height;

            // Calculate line equations.
            float aTop = (tr.y - tl.y) / (tr.x - tl.x);
            float aBot = (br.y - bl.y) / (br.x - bl.x);
            float bTop = tr.y - aTop * tr.x;
            float bBot = br.y - aBot * br.x;

            // Generate texture.
            Texture2D l_texture = new Texture2D(width, height);
            l_texture.wrapMode = TextureWrapMode.Clamp;

            // Set the texture.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    l_texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
                float X = x * xR + bl.x;
                for (int y = 0; y < height; y++)
                {
                    float Y = y * yR + yMin;
                    if ((Y > aTop * X + bTop) || Y < aBot * X + bBot)
                    {
                        l_texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                    else
                    {
                        l_texture.SetPixel(x, y, new Color(1, 1, 1, 1));
                    }
                }
            }

            // Apply change.
            l_texture.Apply();
            return l_texture;
        }
        #endregion
    }
}