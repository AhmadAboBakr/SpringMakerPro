using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Generates a spring along a quadratic Bézier curve defined by three control points
    /// </summary>
    public class CurvedSpring : BaseSpring
    {
        [Header("Path Settings")]
        [SerializeField, Tooltip("Start control point of the curved path")]
        private Vector3 startPoint = Vector3.zero;

        [SerializeField, Tooltip("Middle control point of the curved path")]
        private Vector3 middlePoint = new Vector3(2f, 1f, 0f);

        [SerializeField, Tooltip("End control point of the curved path")]
        private Vector3 endPoint = new Vector3(4f, 0f, 0f);

        #region Public API

        /// <summary>
        /// Gets or sets the start control point
        /// </summary>
        public Vector3 StartPoint
        {
            get => startPoint;
            set
            {
                if (startPoint == value) return;
                startPoint = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the middle control point
        /// </summary>
        public Vector3 MiddlePoint
        {
            get => middlePoint;
            set
            {
                if (middlePoint == value) return;
                middlePoint = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the end control point
        /// </summary>
        public Vector3 EndPoint
        {
            get => endPoint;
            set
            {
                if (endPoint == value) return;
                endPoint = value;
                MarkDirty();
            }
        }

        #endregion

        /// <summary>
        /// Generates spring points along the quadratic Bézier path
        /// </summary>
        public override void CalculatePoints()
        {
            controlPoints = new Vector3[TotalPoints];

            for (int i = 0; i < TotalPoints; i++)
            {
                float t = (float)i / Mathf.Max(1, TotalPoints - 1);
                Vector3 pathPoint = EvaluateBezier(t);
                Vector3 tangent = EvaluateBezierTangent(t).normalized;

                Vector3 perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;
                if (perpendicular.sqrMagnitude < 0.01f)
                    perpendicular = Vector3.Cross(tangent, Vector3.forward).normalized;

                Vector3 binormal = Vector3.Cross(tangent, perpendicular).normalized;

                float angle = 2f * Mathf.PI * windings * t;
                float taperRadius = radius * EvaluateTaper(t);
                Vector3 springOffset = perpendicular * (taperRadius * Mathf.Cos(angle))
                                     + binormal * (taperRadius * Mathf.Sin(angle));

                controlPoints[i] = pathPoint + springOffset;
            }
        }

        /// <summary>
        /// Evaluates the quadratic Bézier curve at parameter t
        /// </summary>
        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Point on the Bézier curve</returns>
        public Vector3 EvaluateBezier(float t)
        {
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * startPoint
                 + 2f * oneMinusT * t * middlePoint
                 + t * t * endPoint;
        }

        /// <summary>
        /// Evaluates the tangent of the quadratic Bézier curve at parameter t
        /// </summary>
        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Tangent vector at the given point</returns>
        public Vector3 EvaluateBezierTangent(float t)
        {
            return 2f * (1f - t) * (middlePoint - startPoint)
                 + 2f * t * (endPoint - middlePoint);
        }

        #region Gizmos

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            DrawControlPointGizmos();
            DrawCurveGizmos();
        }

        private void DrawControlPointGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(startPoint), 0.1f);
            Gizmos.DrawWireSphere(transform.TransformPoint(middlePoint), 0.1f);
            Gizmos.DrawWireSphere(transform.TransformPoint(endPoint), 0.1f);
        }

        private void DrawCurveGizmos()
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 20; i++)
            {
                float t1 = (float)i / 19f;
                float t2 = (float)(i + 1) / 19f;
                Gizmos.DrawLine(
                    transform.TransformPoint(EvaluateBezier(t1)),
                    transform.TransformPoint(EvaluateBezier(t2)));
            }
        }

        #endregion
    }
}
