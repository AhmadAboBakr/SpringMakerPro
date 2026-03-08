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

        private float _lastRadius;
        private int _lastWindings;
        private int _lastPointsPerWinding;

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
        /// Detects changes to serialized fields made outside property setters (e.g. Unity Animator).
        /// Subclasses should override and combine with base to detect their own fields.
        /// </summary>
        protected virtual bool CheckExternalChanges()
        {
            return !Mathf.Approximately(radius, _lastRadius)
                || windings != _lastWindings
                || pointsPerWinding != _lastPointsPerWinding;
        }

        /// <summary>
        /// Snapshots current field values for external change detection.
        /// Subclasses should override and call base to snapshot their own fields.
        /// </summary>
        protected virtual void SnapshotExternalState()
        {
            _lastRadius = radius;
            _lastWindings = windings;
            _lastPointsPerWinding = pointsPerWinding;
        }

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

        protected virtual void LateUpdate()
        {
            if (NeedsRecalculation)
            {
                isDirty = false;
                SnapshotExternalState();
                CalculatePoints();
                OnSpringUpdated?.Invoke(controlPoints);
            }
        }

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
