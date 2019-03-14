using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Primitives/CurveRenderer")]
    [RequireComponent(typeof(RectTransform))]
    public class CurveRenderer : UIPrimitiveBase
    {
        private enum SegmentType
        {
            Start,
            Middle,
            End,
            Full,
        }
        public enum JoinType
        {
            Bevel,
            Miter
        }
        // A bevel 'nice' join displaces the vertices of the line segment instead of simply rendering a
        // quad to connect the endpoints. This improves the look of textured and transparent lines, since
        // there is no overlapping.
        private const float MIN_BEVEL_NICE_JOIN = 30 * Mathf.Deg2Rad;
        private const float MIN_MITER_JOIN = 15 * Mathf.Deg2Rad;

        private static readonly Vector2 UV_TOP_LEFT = Vector2.zero;
        private static readonly Vector2 UV_BOTTOM_LEFT = new Vector2(0, 1);
        private static readonly Vector2 UV_TOP_CENTER = new Vector2(0.5f, 0);
        private static readonly Vector2 UV_BOTTOM_CENTER = new Vector2(0.5f, 1);
        private static readonly Vector2 UV_TOP_RIGHT = new Vector2(1, 0);
        private static readonly Vector2 UV_BOTTOM_RIGHT = new Vector2(1, 1);

        private static readonly Vector2[] startUvs = new[] { UV_TOP_LEFT, UV_BOTTOM_LEFT, UV_BOTTOM_CENTER, UV_TOP_CENTER };
        private static readonly Vector2[] middleUvs = new[] { UV_TOP_CENTER, UV_BOTTOM_CENTER, UV_BOTTOM_CENTER, UV_TOP_CENTER };
        private static readonly Vector2[] endUvs = new[] { UV_TOP_CENTER, UV_BOTTOM_CENTER, UV_BOTTOM_RIGHT, UV_TOP_RIGHT };
        private static readonly Vector2[] fullUvs = new[] { UV_TOP_LEFT, UV_BOTTOM_LEFT, UV_BOTTOM_RIGHT, UV_TOP_RIGHT };


        [SerializeField, Tooltip("Points to draw lines between\n Can be improved using the Resolution Option")]
        private Vector2[] m_points;
        /// <summary>
        /// Points to be drawn in the line.
        /// </summary>
        public Vector2[] Points
        {
            get
            {
                return m_points;
            }

            set
            {
                if (SetPropertyUtility.SetClass(ref m_points, value))
                {
                    SetAllDirty();
                }
            }
        }

        [SerializeField, Tooltip("Thickness of the line")]
        internal float m_LineThickness = 2;

        public float LineThickness
        {
            get
            {
                return m_LineThickness;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_LineThickness, value))
                {
                    SetAllDirty();
                }
            }
        }

        [Tooltip("The type of Join used between lines, Square/Mitre or Curved/Bevel")]
        public JoinType LineJoins = JoinType.Bevel;

        [HideInInspector]
        public bool drivenExternally = false;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (m_points == null || m_points.Length < 2) return;

            Vector2[] pointsToDraw = ImproveResolution == ResolutionMode.None ? m_points : IncreaseResolution(m_points);

            // Generate the quads that make up the wide line
            var segments = new List<UIVertex[]>();
            Vector2 start, end;
            if (pointsToDraw.Length == 2)
            {
                start = pointsToDraw[0] - rectTransform.pivot;
                end = pointsToDraw[1] - rectTransform.pivot;
                segments.Add(CreateLineSegment(start, end, SegmentType.Full));
            }
            else
            {
                start = pointsToDraw[0] - rectTransform.pivot;
                end = pointsToDraw[1] - rectTransform.pivot;
                segments.Add(CreateLineSegment(start, end, SegmentType.Start));
                for (var i = 2; i < pointsToDraw.Length - 1; i++)
                {
                    start = pointsToDraw[i - 1] - rectTransform.pivot;
                    end = pointsToDraw[i] - rectTransform.pivot;
                    segments.Add(CreateLineSegment(start, end, SegmentType.Middle));
                }
                start = pointsToDraw[pointsToDraw.Length - 2] - rectTransform.pivot;
                end = pointsToDraw[pointsToDraw.Length - 1] - rectTransform.pivot;
                segments.Add(CreateLineSegment(start, end, SegmentType.End));
            }


            vh.Clear();

            // Add the line segments to the vertex helper, creating any joins as needed
            int max = segments.Count - 1;
            for (var i = 0; i < max; i++)
            {
                var vec1 = segments[i][1].position - segments[i][2].position;
                var vec2 = segments[i + 1][2].position - segments[i + 1][1].position;
                var angle = Vector2.Angle(vec1, vec2) * Mathf.Deg2Rad;

                // Positive sign means the line is turning in a 'clockwise' direction
                var sign = Mathf.Sign(Vector3.Cross(vec1.normalized, vec2.normalized).z);

                // Calculate the miter point
                var miterDistance = m_LineThickness / (2 * Mathf.Tan(angle / 2));
                var miterPointA = segments[i][2].position - vec1.normalized * miterDistance * sign;
                var miterPointB = segments[i][3].position + vec1.normalized * miterDistance * sign;

                var joinType = LineJoins;
                if (joinType == JoinType.Miter)
                {
                    // Make sure we can make a miter join without too many artifacts.
                    if (miterDistance < vec1.magnitude / 2 && miterDistance < vec2.magnitude / 2 && angle > MIN_MITER_JOIN)
                    {
                        segments[i][2].position = miterPointA;
                        segments[i][3].position = miterPointB;
                        segments[i + 1][0].position = miterPointB;
                        segments[i + 1][1].position = miterPointA;
                    }
                    else
                    {
                        joinType = JoinType.Bevel;
                    }
                }

                if (joinType == JoinType.Bevel)
                {
                    if (miterDistance < vec1.magnitude / 2 && miterDistance < vec2.magnitude / 2 && angle > MIN_BEVEL_NICE_JOIN)
                    {
                        if (sign < 0)
                        {
                            segments[i][2].position = miterPointA;
                            segments[i + 1][1].position = miterPointA;
                        }
                        else
                        {
                            segments[i][3].position = miterPointB;
                            segments[i + 1][0].position = miterPointB;
                        }
                    }

                    var join = new UIVertex[] { segments[i][2], segments[i][3], segments[i + 1][0], segments[i + 1][1] };
                    vh.AddUIVertexQuad(join);
                }
                vh.AddUIVertexQuad(segments[i]);
            }
            vh.AddUIVertexQuad(segments[max]);
            if (vh.currentVertCount > 64000)
            {
                Debug.LogError("Max Verticies size is 64000, current mesh vertcies count is [" + vh.currentVertCount + "] - Cannot Draw");
                vh.Clear();
                return;
            }
        }

        private UIVertex[] CreateLineSegment(Vector2 start, Vector2 end, SegmentType type)
        {
            Vector2 startOffset, endOffset;
            Vector2[] uvs;
            Vector2 normal = new Vector2(start.y - end.y, end.x - start.x).normalized;
            float dot = Vector2.Dot(normal, Vector2.up);
            float vertical = (m_LineThickness / 2) / dot;

            startOffset = new Vector2(start.y - end.y, end.x - start.x).normalized * m_LineThickness / 2;
            endOffset = new Vector2(start.y - end.y, end.x - start.x).normalized * m_LineThickness / 2;
            switch (type)
            {
                case SegmentType.Start:
                    startOffset = vertical * Vector2.up;
                    uvs = startUvs;
                    break;
                case SegmentType.End:
                    endOffset = vertical * Vector2.up;
                    uvs = endUvs;
                    break;
                case SegmentType.Full:
                    startOffset = vertical * Vector2.up;
                    endOffset = vertical * Vector2.up;
                    uvs = fullUvs;
                    break;
                default:
                    uvs = middleUvs;
                    break;
            }
            return SetVbo(new[] { start - startOffset, start + startOffset, end + endOffset, end - endOffset }, uvs);
        }

        protected override void ResolutionToNativeSize(float distance)
        {
            if (UseNativeSize)
            {
                m_Resolution = distance / (activeSprite.rect.width / pixelsPerUnit);
                m_LineThickness = activeSprite.rect.height / pixelsPerUnit;
            }
        }
    }
}