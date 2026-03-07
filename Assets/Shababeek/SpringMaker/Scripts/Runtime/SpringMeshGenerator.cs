using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Generates a tube mesh that follows a BaseSpring's coil path
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SpringMeshGenerator : MonoBehaviour
    {
        [Header("Spring Reference")]
        [SerializeField, Tooltip("The spring to generate a mesh for (auto-detected if empty)")]
        private BaseSpring spring;

        [Header("Tube Settings")]
        [SerializeField, Tooltip("Number of sides on the tube cross-section (3=triangle, 4=square, 6=hex, 16+=smooth)")]
        [Range(3, 32)]
        private int sides = 8;

        [SerializeField, Tooltip("Radius of the tube cross-section")]
        private float tubeRadius = 0.05f;

        [SerializeField, Tooltip("Tube radius curve from start (0) to end (1) of the spring")]
        private AnimationCurve tubeRadiusCurve = AnimationCurve.Constant(0f, 1f, 1f);

        [Header("Mesh Options")]
        [SerializeField, Tooltip("Number of Catmull-Rom subdivisions between spring points (1 = no smoothing)")]
        [Range(1, 10)]
        private int subdivisions = 3;

        [SerializeField, Tooltip("Close the tube ends with cap geometry")]
        private bool caps = true;

        [SerializeField, Tooltip("Generate tangent data for normal mapping")]
        private bool generateTangents = true;

        private MeshFilter _meshFilter;
        private Mesh _mesh;

        #region Public API

        /// <summary>
        /// Gets or sets the tube cross-section side count
        /// </summary>
        public int Sides
        {
            get => sides;
            set { sides = Mathf.Max(3, value); RegenerateMesh(); }
        }

        /// <summary>
        /// Gets or sets the tube radius
        /// </summary>
        public float TubeRadius
        {
            get => tubeRadius;
            set { tubeRadius = Mathf.Max(0.001f, value); RegenerateMesh(); }
        }

        /// <summary>
        /// Forces immediate mesh regeneration
        /// </summary>
        public void RegenerateMesh()
        {
            if (spring == null || _meshFilter == null) return;
            BuildMesh(spring.ControlPoints);
        }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh { name = "SpringTubeMesh" };
            _meshFilter.sharedMesh = _mesh;

            if (spring == null)
                spring = GetComponentInChildren<BaseSpring>();

            if (spring == null)
            {
                Debug.LogWarning("SpringMeshGenerator: No BaseSpring assigned or found.", this);
                return;
            }

            spring.OnSpringUpdated += HandleSpringUpdated;
        }

        private void OnDestroy()
        {
            if (spring != null)
                spring.OnSpringUpdated -= HandleSpringUpdated;

            if (_mesh != null)
                DestroyImmediate(_mesh);
        }

        private void Start()
        {
            if (spring != null)
                BuildMesh(spring.ControlPoints);
        }

        private void Update()
        {
            if (spring != null && spring.NeedsRecalculation)
                BuildMesh(spring.ControlPoints);
        }

        private void OnValidate()
        {
            sides = Mathf.Max(3, sides);
            tubeRadius = Mathf.Max(0.001f, tubeRadius);
            subdivisions = Mathf.Max(1, subdivisions);
        }

        #endregion

        private void HandleSpringUpdated(Vector3[] points)
        {
            BuildMesh(points);
        }

        private void BuildMesh(Vector3[] springPoints)
        {
            if (springPoints == null || springPoints.Length < 2) return;

            Vector3[] path = subdivisions > 1
                ? CatmullRomSmooth(springPoints, subdivisions)
                : springPoints;

            int ringCount = path.Length;
            int vertsPerRing = sides + 1;
            int bodyVertCount = ringCount * vertsPerRing;
            int capVertCount = caps ? (sides + 1) * 2 : 0;
            int totalVerts = bodyVertCount + capVertCount;

            Vector3[] vertices = new Vector3[totalVerts];
            Vector3[] normals = new Vector3[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];

            int bodyTriCount = (ringCount - 1) * sides * 6;
            int capTriCount = caps ? sides * 3 * 2 : 0;
            int[] triangles = new int[bodyTriCount + capTriCount];

            for (int ring = 0; ring < ringCount; ring++)
            {
                float t = (float)ring / (ringCount - 1);
                Vector3 position = path[ring];

                Vector3 forward;
                if (ring < ringCount - 1)
                    forward = (path[ring + 1] - path[ring]).normalized;
                else
                    forward = (path[ring] - path[ring - 1]).normalized;

                Vector3 up = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f)
                    up = Vector3.right;

                Vector3 right = Vector3.Cross(forward, up).normalized;
                up = Vector3.Cross(right, forward).normalized;

                float curveMultiplier = tubeRadiusCurve != null ? tubeRadiusCurve.Evaluate(t) : 1f;
                float r = tubeRadius * curveMultiplier;

                for (int s = 0; s <= sides; s++)
                {
                    float angle = (float)s / sides * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    Vector3 offset = right * (cos * r) + up * (sin * r);
                    Vector3 normal = (right * cos + up * sin).normalized;

                    int idx = ring * vertsPerRing + s;
                    vertices[idx] = position + offset;
                    normals[idx] = normal;
                    uvs[idx] = new Vector2((float)s / sides, t);
                }
            }

            int tri = 0;
            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int current = ring * vertsPerRing + s;
                    int next = current + vertsPerRing;

                    triangles[tri++] = current;
                    triangles[tri++] = next;
                    triangles[tri++] = current + 1;

                    triangles[tri++] = current + 1;
                    triangles[tri++] = next;
                    triangles[tri++] = next + 1;
                }
            }

            if (caps)
                BuildCaps(vertices, normals, uvs, triangles, path, bodyVertCount, tri);

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;

            if (generateTangents)
                _mesh.RecalculateTangents();

            _mesh.RecalculateBounds();
        }

        private void BuildCaps(Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
            int[] triangles, Vector3[] path, int vertOffset, int triOffset)
        {
            Vector3 startPos = path[0];
            Vector3 endPos = path[path.Length - 1];
            Vector3 startDir = (path[1] - path[0]).normalized;
            Vector3 endDir = (path[path.Length - 1] - path[path.Length - 2]).normalized;

            int capStart = vertOffset;
            int capEnd = vertOffset + sides + 1;

            vertices[capStart] = startPos;
            normals[capStart] = -startDir;
            uvs[capStart] = new Vector2(0.5f, 0f);

            for (int s = 0; s < sides; s++)
            {
                int bodyIdx = s;
                vertices[capStart + 1 + s] = vertices[bodyIdx];
                normals[capStart + 1 + s] = -startDir;
                uvs[capStart + 1 + s] = uvs[bodyIdx];
            }

            int vertsPerRing = sides + 1;
            int lastRing = (path.Length - 1) * vertsPerRing;

            vertices[capEnd] = endPos;
            normals[capEnd] = endDir;
            uvs[capEnd] = new Vector2(0.5f, 1f);

            for (int s = 0; s < sides; s++)
            {
                int bodyIdx = lastRing + s;
                vertices[capEnd + 1 + s] = vertices[bodyIdx];
                normals[capEnd + 1 + s] = endDir;
                uvs[capEnd + 1 + s] = uvs[bodyIdx];
            }

            int tri = triOffset;

            for (int s = 0; s < sides; s++)
            {
                int next = (s + 1) % sides;
                triangles[tri++] = capStart;
                triangles[tri++] = capStart + 1 + next;
                triangles[tri++] = capStart + 1 + s;
            }

            for (int s = 0; s < sides; s++)
            {
                int next = (s + 1) % sides;
                triangles[tri++] = capEnd;
                triangles[tri++] = capEnd + 1 + s;
                triangles[tri++] = capEnd + 1 + next;
            }
        }

        #region Catmull-Rom Smoothing

        private static Vector3[] CatmullRomSmooth(Vector3[] points, int subs)
        {
            if (points.Length < 2) return points;

            int segCount = points.Length - 1;
            int total = segCount * subs + 1;
            Vector3[] result = new Vector3[total];

            for (int i = 0; i < segCount; i++)
            {
                Vector3 p0 = i > 0 ? points[i - 1] : points[i];
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];
                Vector3 p3 = i + 2 < points.Length ? points[i + 2] : points[i + 1];

                for (int j = 0; j < subs; j++)
                {
                    float t = (float)j / subs;
                    result[i * subs + j] = EvalCatmullRom(p0, p1, p2, p3, t);
                }
            }

            result[total - 1] = points[points.Length - 1];
            return result;
        }

        private static Vector3 EvalCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        #endregion
    }
}
