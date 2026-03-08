using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Generates a spring along a cubic Bézier curve between two transforms
    /// </summary>
    public class TransformSpring : BaseSpring
    {
        [Header("Transform References")]
        [SerializeField, Tooltip("Start transform of the spring")]
        private Transform t1;

        [SerializeField, Tooltip("End transform of the spring")]
        private Transform t2;

        [Header("Direction Control")]
        [SerializeField, Tooltip("How far the Bézier control handles extend from each transform")]
        private float directionDistance = 1f;

        private Vector3 _snapshotP1;
        private Vector3 _snapshotP2;
        private Quaternion _snapshotR1;
        private Quaternion _snapshotR2;

        #region Public API

        /// <summary>
        /// Gets or sets the direction control distance
        /// </summary>
        public float DirectionDistance
        {
            get => directionDistance;
            set
            {
                if (Mathf.Approximately(directionDistance, value)) return;
                directionDistance = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets the first transform reference
        /// </summary>
        public Transform StartTransform => t1;

        /// <summary>
        /// Gets the second transform reference
        /// </summary>
        public Transform EndTransform => t2;

        /// <summary>
        /// Gets the four Bézier control points in world space
        /// </summary>
        /// <returns>Array of 4 control points, or null if transforms are unassigned</returns>
        public Vector3[] GetBezierControlPoints()
        {
            if (t1 == null || t2 == null) return null;

            return new[]
            {
                t1.position,
                t1.position + t1.up * directionDistance,
                t2.position - t2.up * directionDistance,
                t2.position
            };
        }

        #endregion

        /// <summary>
        /// Generates spring points along the cubic Bézier curve
        /// </summary>
        public override void CalculatePoints()
        {
            if (t1 == null || t2 == null) return;

            controlPoints = new Vector3[TotalPoints];

            for (int i = 0; i < TotalPoints; i++)
            {
                float t = (float)i / Mathf.Max(1, TotalPoints - 1);
                Vector3 pathPoint = EvaluateCubicBezier(t);
                Vector3 tangent = EvaluateCubicBezierTangent(t).normalized;

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

        #region Bézier Helpers

        /// <summary>
        /// Evaluates the cubic Bézier curve at parameter t in local space
        /// </summary>
        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Point on the Bézier curve in local space</returns>
        public Vector3 EvaluateCubicBezier(float t)
        {
            if (t1 == null || t2 == null) return Vector3.zero;

            Vector3 p0 = transform.InverseTransformPoint(t1.position);
            Vector3 p1 = transform.InverseTransformPoint(t1.position + t1.up * directionDistance);
            Vector3 p2 = transform.InverseTransformPoint(t2.position - t2.up * directionDistance);
            Vector3 p3 = transform.InverseTransformPoint(t2.position);

            float u = 1f - t;
            return u * u * u * p0
                 + 3f * u * u * t * p1
                 + 3f * u * t * t * p2
                 + t * t * t * p3;
        }

        /// <summary>
        /// Evaluates the tangent of the cubic Bézier curve at parameter t in local space
        /// </summary>
        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Tangent vector in local space</returns>
        public Vector3 EvaluateCubicBezierTangent(float t)
        {
            if (t1 == null || t2 == null) return Vector3.forward;

            Vector3 p0 = transform.InverseTransformPoint(t1.position);
            Vector3 p1 = transform.InverseTransformPoint(t1.position + t1.up * directionDistance);
            Vector3 p2 = transform.InverseTransformPoint(t2.position - t2.up * directionDistance);
            Vector3 p3 = transform.InverseTransformPoint(t2.position);

            float u = 1f - t;
            return 3f * u * u * (p1 - p0)
                 + 6f * u * t * (p2 - p1)
                 + 3f * t * t * (p3 - p2);
        }

        #endregion

        #region External Change Detection

        protected override bool CheckExternalChanges()
        {
            if (base.CheckExternalChanges()) return true;
            if (t1 == null || t2 == null) return false;
            return t1.position != _snapshotP1
                || t2.position != _snapshotP2
                || t1.rotation != _snapshotR1
                || t2.rotation != _snapshotR2;
        }

        protected override void SnapshotExternalState()
        {
            base.SnapshotExternalState();
            if (t1 == null || t2 == null) return;
            _snapshotP1 = t1.position;
            _snapshotP2 = t2.position;
            _snapshotR1 = t1.rotation;
            _snapshotR2 = t2.rotation;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (t1 == null || t2 == null) return;

            DrawBezierControlPointGizmos();
            DrawBezierCurveGizmos();
        }

        private void DrawBezierControlPointGizmos()
        {
            Vector3[] cp = GetBezierControlPoints();
            if (cp == null) return;

            Gizmos.color = Color.red;
            foreach (Vector3 point in cp)
                Gizmos.DrawWireSphere(point, 0.1f);
        }

        private void DrawBezierCurveGizmos()
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 50; i++)
            {
                float a = (float)i / 49f;
                float b = (float)(i + 1) / 49f;
                Gizmos.DrawLine(
                    transform.TransformPoint(EvaluateCubicBezier(a)),
                    transform.TransformPoint(EvaluateCubicBezier(b)));
            }
        }

        #endregion
    }
}
