using System;
using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Base class for procedural spring generation
    /// </summary>
    public abstract class BaseSpring : MonoBehaviour
    {
        [Header("Spring Settings")]
        [SerializeField, Tooltip("Number of windings in the spring")]
        protected int windings = 5;

        [SerializeField, Tooltip("Radius of the spring coil")]
        protected float radius = 0.5f;

        [SerializeField, Tooltip("Number of points per winding")]
        protected int pointsPerWinding = 6;

        [SerializeField, Tooltip("Radius taper curve from start (0) to end (1) of the spring")]
        protected AnimationCurve taperCurve = AnimationCurve.Constant(0f, 1f, 1f);

        /// <summary>
        /// Calculated points that define the spring shape
        /// </summary>
        protected Vector3[] controlPoints;

        /// <summary>
        /// Dirty flag set whenever any property changes
        /// </summary>
        protected bool isDirty = true;

        /// <summary>
        /// Fires after spring points are recalculated
        /// </summary>
        public event Action<Vector3[]> OnSpringUpdated;

        #region Public API

        /// <summary>
        /// Gets or sets the coil radius
        /// </summary>
        public float Radius
        {
            get => radius;
            set
            {
                if (Mathf.Approximately(radius, value)) return;
                radius = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the points per winding
        /// </summary>
        public int PointsPerWinding
        {
            get => pointsPerWinding;
            set
            {
                if (pointsPerWinding == value) return;
                pointsPerWinding = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the number of windings
        /// </summary>
        public int Windings
        {
            get => windings;
            set
            {
                if (windings == value) return;
                windings = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the taper curve applied to the radius
        /// </summary>
        public AnimationCurve TaperCurve
        {
            get => taperCurve;
            set
            {
                taperCurve = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// True when points need recalculating
        /// </summary>
        public bool NeedsRecalculation => isDirty || CheckExternalChanges();

        /// <summary>
        /// Total number of calculated points
        /// </summary>
        public int NumberOfPoints => TotalPoints;

        /// <summary>
        /// Gets the control points, recalculating if dirty
        /// </summary>
        public Vector3[] ControlPoints
        {
            get
            {
                if (NeedsRecalculation)
                {
                    isDirty = false;
                    SnapshotExternalState();
                    CalculatePoints();
                    OnSpringUpdated?.Invoke(controlPoints);
                }
                return controlPoints;
            }
        }

        /// <summary>
        /// Forces a recalculation on the next access
        /// </summary>
        public void SetDirty()
        {
            MarkDirty();
        }

        #endregion

        #region Protected Helpers

        /// <summary>
        /// Total point count derived from windings and resolution
        /// </summary>
        protected int TotalPoints => windings * pointsPerWinding;

        /// <summary>
        /// Marks the spring as needing recalculation
        /// </summary>
        protected void MarkDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// Override to detect changes outside serialized fields (e.g. transform movement)
        /// </summary>
        protected virtual bool CheckExternalChanges() => false;

        /// <summary>
        /// Override to snapshot external state after recalculation (e.g. store current transform positions)
        /// </summary>
        protected virtual void SnapshotExternalState() { }

        /// <summary>
        /// Evaluates the taper multiplier at a normalized position
        /// </summary>
        /// <param name="t">Normalized position along the spring (0–1)</param>
        /// <returns>Radius multiplier</returns>
        protected float EvaluateTaper(float t)
        {
            if (taperCurve == null || taperCurve.length == 0) return 1f;
            return taperCurve.Evaluate(t);
        }

        #endregion

        #region Abstract

        /// <summary>
        /// Recalculates all spring points
        /// </summary>
        public abstract void CalculatePoints();

        #endregion

        #region Unity Callbacks

        protected virtual void OnValidate()
        {
            windings = Mathf.Max(1, windings);
            pointsPerWinding = Mathf.Max(3, pointsPerWinding);
            radius = Mathf.Max(0.01f, radius);
            MarkDirty();
        }

        protected virtual void OnDrawGizmos()
        {
            DrawSpringGizmos();
        }

        #endregion

        #region Gizmos

        /// <summary>
        /// Draws the spring wireframe in the Scene view
        /// </summary>
        protected virtual void DrawSpringGizmos()
        {
            Gizmos.color = Color.cyan;

            Vector3[] points = ControlPoints;
            if (points == null || points.Length < 2) return;

            for (int i = 1; i < points.Length; i++)
            {
                Gizmos.DrawLine(
                    transform.TransformPoint(points[i - 1]),
                    transform.TransformPoint(points[i]));
            }
        }

        #endregion
    }
}
