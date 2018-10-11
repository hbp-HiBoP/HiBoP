using System.Collections.Generic;
using UnityEngine;

namespace Tools.Unity
{
    public static class Vector2Extension
    {
        public static Vector2 MultiplyByElements(IEnumerable<Vector2> vectors)
        {
            float x = 1, y = 1;
            foreach (var vector in vectors)
            {
                x *= vector.x;
                y *= vector.y;
            }
            return new Vector2(x, y);
        }
        public static Vector2 MultiplyByElements(Vector2 v1, Vector2 v2)
        {
            return MultiplyByElements(new Vector2[] { v1, v2 });
        }
        public static Vector2 Abs(this Vector2 vector)
        {
            return new Vector2(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
        }
        public static float Range(this Vector2 vector)
        {
            return vector.y - vector.x;
        }
    }
}

