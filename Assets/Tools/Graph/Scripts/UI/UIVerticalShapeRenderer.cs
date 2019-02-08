/// Credit adrien Gannerie
using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Primitives/UIVerticalShapeRenderer")]
    public class UIVerticalShapeRenderer : UIPrimitiveBase
    {
        private enum SegmentType
        {
            Start,
            Middle,
            End,
        }

        private static readonly Vector2 UV_TOP_LEFT = Vector2.zero;
        private static readonly Vector2 UV_BOTTOM_LEFT = new Vector2(0, 1);
        private static readonly Vector2 UV_TOP_CENTER = new Vector2(0.5f, 0);
        private static readonly Vector2 UV_BOTTOM_CENTER = new Vector2(0.5f, 1);
        private static readonly Vector2 UV_TOP_RIGHT = new Vector2(1, 0);
        private static readonly Vector2 UV_BOTTOM_RIGHT = new Vector2(1, 1);

        private static readonly Vector2[] startUvs = new[] { UV_TOP_LEFT, UV_BOTTOM_LEFT, UV_BOTTOM_CENTER, UV_TOP_CENTER };
        private static readonly Vector2[] middleUvs = new[] { UV_TOP_CENTER, UV_BOTTOM_CENTER, UV_BOTTOM_CENTER, UV_TOP_CENTER };
        private static readonly Vector2[] endUvs = new[] { UV_TOP_CENTER, UV_BOTTOM_CENTER, UV_BOTTOM_RIGHT, UV_TOP_RIGHT };



        [SerializeField, Tooltip("Points to draw lines between\n Can be improved using the Resolution Option")]
        internal Vector2[] m_Points;
        /// <summary>
        /// Points to be drawn in the line.
        /// </summary>
        public Vector2[] Points
        {
            get
            {
                return m_Points;
            }
            set
            {
                if (m_Points == value)
                    return;
                m_Points = value;
                SetAllDirty();
            }
        }

        [SerializeField, Tooltip("Thickness of the shape")]
        internal float[] m_ShapeThickness;
        /// <summary>
        /// Shapes thickness to be drawn in the line.
        /// </summary>
        public float[] ShapeThickness
        {
            get
            {
                return m_ShapeThickness;
            }
            set
            {
                if (m_ShapeThickness == value)
                    return;
                m_ShapeThickness = value;
                SetAllDirty();
            }
        }

        [SerializeField, Tooltip("Use the relative bounds of the Rect Transform (0,0 -> 0,1) or screen space coordinates")]
        internal bool m_relativeSize;
        public bool RelativeSize
        {
            get { return m_relativeSize; }
            set { m_relativeSize = value; SetAllDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (m_Points == null || m_ShapeThickness == null || m_Points.Length != m_ShapeThickness.Length)
                return;

            float width = !m_relativeSize ? 1 : rectTransform.rect.width;
            float height = !m_relativeSize ? 1 : rectTransform.rect.height;
            float widthOffset = -rectTransform.pivot.x * width;
            float heightOffset = -rectTransform.pivot.y * height;

            vh.Clear();

            // Generate the quads that make up the wide line
            for (var i = 1; i < m_Points.Length; i++)
            {
                var start = m_Points[i - 1];
                var end = m_Points[i];
                var startThickness = m_ShapeThickness[i - 1] * width;
                var endThickness = m_ShapeThickness[i] * height;
                start = new Vector2(start.x * width + widthOffset, start.y * height + heightOffset);
                end = new Vector2(end.x * width + widthOffset, end.y * height + heightOffset);
                vh.AddUIVertexQuad(CreateShapeSegment(start, end, startThickness, endThickness));
            }
        }
        private UIVertex[] CreateShapeSegment(Vector2 start, Vector2 end, float startThickness, float endThickness)
        {
            Vector2[] uvs = middleUvs;
            Vector2 offsetStart = Vector2.up * startThickness / 2;
            Vector2 offsetEnd = Vector2.up * endThickness / 2;
            Vector2 v1 = start - offsetStart;
            Vector2 v2 = start + offsetStart;
            Vector2 v3 = end + offsetEnd;
            Vector2 v4 = end - offsetEnd;
            return SetVbo(new[] { v1, v2, v3, v4 }, uvs);
        }
    }
}