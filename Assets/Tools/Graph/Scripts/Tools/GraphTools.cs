using UnityEngine;

namespace Tools.Unity.Graph
{
    public static class GraphTools
    {
        public static Vector2 GetRatio(this Rect rect, Limits limits)
        {
            return new Vector2(rect.width / (limits.AbscissaMax - limits.AbscissaMin), rect.height / (limits.OrdinateMax - limits.OrdinateMin));
        }
        public static Vector2 GetLocalPosition(this Vector2 position, Vector2 origin, Vector2 ratio)
        {
            return new Vector2((position.x - origin.x) * ratio.x, (position.y - origin.y) * ratio.y);
        }
        public static Vector2 GetLocalPosition(this Rect rect, Vector2 position, Limits limits)
        {
            Vector2 ratio = rect.GetRatio(limits);
            return new Vector2((position.x - limits.AbscissaMin) * ratio.x, (position.y - limits.OrdinateMin) * ratio.y);
        }
    }
}