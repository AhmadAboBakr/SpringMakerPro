using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Renders a BaseSpring using a LineRenderer with optional Catmull-Rom smoothing
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LineRendererSpring : MonoBehaviour
    {
        [Header("Spring Reference")]
        [SerializeField, Tooltip("The spring component to visualize")]
        private BaseSpring spring;

        [Header("Visual Settings")]
        [SerializeField, Tooltip("Catmull-Rom subdivisions between each pair of spring points (1 = no smoothing)")]
        [Range(1, 20)]
        private int smoothingSegments = 10;

        [SerializeField, Tooltip("Width of the rendered line")]
        private float lineWidth = 0.1f;

        private LineRenderer _lineRenderer;

        #region Unity Callbacks

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();

            if (spring == null)
                spring = GetComponentInChildren<BaseSpring>();

            if (spring == null)
            {
                Debug.LogWarning("LineRendererSpring: No BaseSpring assigned or found in children.", this);
                return;
            }

            spring.OnSpringUpdated += HandleSpringUpdated;
        }

        private void OnDestroy()
        {
            if (spring != null)
                spring.OnSpringUpdated -= HandleSpringUpdated;
        }

        private void Start()
        {
            if (spring != null)
                RefreshLine();
        }

        private void Update()
        {
            if (spring != null && spring.NeedsRecalculation)
                RefreshLine();
        }

        private void LateUpdate()
        {
            if (spring != null && spring.NeedsRecalculation)
                RefreshLine();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Forces the LineRenderer to update immediately
        /// </summary>
        public void RefreshLine()
        {
            if (spring == null || _lineRenderer == null) return;

            Vector3[] springPoints = spring.ControlPoints;
            if (springPoints == null || springPoints.Length < 2) return;

            Vector3[] worldPoints = TransformPointsToWorld(springPoints);
            Vector3[] finalPoints = smoothingSegments > 1
                ? CatmullRomSmooth(worldPoints, smoothingSegments)
                : worldPoints;

            _lineRenderer.positionCount = finalPoints.Length;
            _lineRenderer.SetPositions(finalPoints);
        }

        #endregion

        private void HandleSpringUpdated(Vector3[] points)
        {
            RefreshLine();
        }

        private void ConfigureLineRenderer()
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.generateLightingData = true;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.allowOcclusionWhenDynamic = false;
            _lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
        }

        private Vector3[] TransformPointsToWorld(Vector3[] localPoints)
        {
            Vector3[] world = new Vector3[localPoints.Length];
            for (int i = 0; i < localPoints.Length; i++)
                world[i] = transform.TransformPoint(localPoints[i]);
            return world;
        }

        /// <summary>
        /// Performs Catmull-Rom interpolation between points for smooth curves
        /// </summary>
        private static Vector3[] CatmullRomSmooth(Vector3[] points, int subdivisions)
        {
            if (points.Length < 2) return points;

            int segmentCount = points.Length - 1;
            int totalPoints = segmentCount * subdivisions + 1;
            Vector3[] smoothed = new Vector3[totalPoints];

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 p0 = i > 0 ? points[i - 1] : points[i];
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];
                Vector3 p3 = i + 2 < points.Length ? points[i + 2] : points[i + 1];

                for (int j = 0; j < subdivisions; j++)
                {
                    float t = (float)j / subdivisions;
                    smoothed[i * subdivisions + j] = CatmullRomPoint(p0, p1, p2, p3, t);
                }
            }

            smoothed[totalPoints - 1] = points[points.Length - 1];
            return smoothed;
        }

        private static Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
