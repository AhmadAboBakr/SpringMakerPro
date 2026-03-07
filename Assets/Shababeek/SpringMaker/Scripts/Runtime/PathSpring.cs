using System.Collections.Generic;
using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Generates a spring along a Catmull-Rom spline defined by multiple control points
    /// </summary>
    public class PathSpring : BaseSpring
    {
        [Header("Path Settings")]
        [SerializeField, Tooltip("Control points that define the spline path")]
        private List<Vector3> pathPoints = new List<Vector3>
        {
            Vector3.zero,
            new Vector3(1f, 0.5f, 0f),
            new Vector3(2f, 1f, 0.5f),
            new Vector3(3f, 0.5f, 1f),
            new Vector3(4f, 0f, 0.5f)
        };

        [SerializeField, Tooltip("Connect the last point back to the first")]
        private bool closedPath;

        [SerializeField, Tooltip("Catmull-Rom alpha parameter (0 = uniform, 0.5 = centripetal, 1 = chordal)")]
        [Range(0f, 1f)]
        private float smoothing = 0.5f;

        #region Public API

        /// <summary>
        /// Number of path control points
        /// </summary>
        public int PathPointCount => pathPoints.Count;

        /// <summary>
        /// Whether the path forms a closed loop
        /// </summary>
        public bool ClosedPath
        {
            get => closedPath;
            set
            {
                if (closedPath == value) return;
                closedPath = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Spline smoothing parameter
        /// </summary>
        public float Smoothing
        {
            get => smoothing;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(smoothing, clamped)) return;
                smoothing = clamped;
                MarkDirty();
            }
        }

        /// <summary>
        /// Adds a point to the end of the path
        /// </summary>
        public void AddPathPoint(Vector3 point)
        {
            pathPoints.Add(point);
            MarkDirty();
        }

        /// <summary>
        /// Removes a point at the given index
        /// </summary>
        public void RemovePathPoint(int index)
        {
            if (index < 0 || index >= pathPoints.Count) return;
            pathPoints.RemoveAt(index);
            MarkDirty();
        }

        /// <summary>
        /// Sets a path point at the given index
        /// </summary>
        public void SetPathPoint(int index, Vector3 point)
        {
            if (index < 0 || index >= pathPoints.Count) return;
            pathPoints[index] = point;
            MarkDirty();
        }

        /// <summary>
        /// Gets a path point at the given index
        /// </summary>
        public Vector3 GetPathPoint(int index)
        {
            if (index < 0 || index >= pathPoints.Count) return Vector3.zero;
            return pathPoints[index];
        }

        /// <summary>
        /// Removes all path points
        /// </summary>
        public void ClearPathPoints()
        {
            pathPoints.Clear();
            MarkDirty();
        }

        #endregion

        /// <summary>
        /// Generates spring points along the Catmull-Rom spline
        /// </summary>
        public override void CalculatePoints()
        {
            if (pathPoints.Count < 2) return;

            controlPoints = new Vector3[TotalPoints];

            for (int i = 0; i < TotalPoints; i++)
            {
                float t = (float)i / Mathf.Max(1, TotalPoints - 1);
                Vector3 pathPoint = EvaluateSpline(t);
                Vector3 tangent = EvaluateSplineTangent(t).normalized;

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

        #region Spline Evaluation

        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Point on the spline</returns>
        public Vector3 EvaluateSpline(float t)
        {
            if (pathPoints.Count == 2)
                return Vector3.Lerp(pathPoints[0], pathPoints[1], t);

            int numSegments = closedPath ? pathPoints.Count : pathPoints.Count - 1;
            float segmentT = t * numSegments;
            int segmentIndex = Mathf.FloorToInt(segmentT);
            float localT = segmentT - segmentIndex;

            if (segmentIndex >= numSegments)
            {
                segmentIndex = numSegments - 1;
                localT = 1f;
            }

            Vector3 p0, p1, p2, p3;

            if (closedPath)
            {
                int count = pathPoints.Count;
                p0 = pathPoints[segmentIndex % count];
                p1 = pathPoints[(segmentIndex + 1) % count];
                p2 = pathPoints[(segmentIndex + 2) % count];
                p3 = pathPoints[(segmentIndex + 3) % count];
            }
            else
            {
                p0 = segmentIndex > 0 ? pathPoints[segmentIndex - 1] : pathPoints[0];
                p1 = pathPoints[segmentIndex];
                p2 = pathPoints[segmentIndex + 1];
                p3 = segmentIndex + 2 < pathPoints.Count
                    ? pathPoints[segmentIndex + 2]
                    : pathPoints[pathPoints.Count - 1];
            }

            return CatmullRom(p0, p1, p2, p3, localT, smoothing);
        }

        /// <param name="t">Parameter value between 0 and 1</param>
        /// <returns>Tangent direction at the given point</returns>
        public Vector3 EvaluateSplineTangent(float t)
        {
            const float delta = 0.01f;
            Vector3 a = EvaluateSpline(Mathf.Max(0f, t - delta));
            Vector3 b = EvaluateSpline(Mathf.Min(1f, t + delta));
            return (b - a).normalized;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha)
        {
            float t0 = 0f;
            float t1 = GetKnotInterval(t0, p0, p1, alpha);
            float t2 = GetKnotInterval(t1, p1, p2, alpha);
            float t3 = GetKnotInterval(t2, p2, p3, alpha);

            t = Mathf.Lerp(t1, t2, t);

            Vector3 a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector3 a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector3 a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

            Vector3 b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
            Vector3 b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;

            return (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;
        }

        private static float GetKnotInterval(float t, Vector3 p0, Vector3 p1, float alpha)
        {
            return Mathf.Pow(Vector3.Distance(p0, p1), alpha) + t;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (pathPoints == null || pathPoints.Count < 2) return;

            Gizmos.color = Color.red;
            foreach (Vector3 point in pathPoints)
                Gizmos.DrawWireSphere(transform.TransformPoint(point), 0.1f);

            Gizmos.color = Color.yellow;
            for (int i = 0; i < 50; i++)
            {
                float a = (float)i / 49f;
                float b = (float)(i + 1) / 49f;
                Gizmos.DrawLine(
                    transform.TransformPoint(EvaluateSpline(a)),
                    transform.TransformPoint(EvaluateSpline(b)));
            }
        }

        #endregion
    }
}
