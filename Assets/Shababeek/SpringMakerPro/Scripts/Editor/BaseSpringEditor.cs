using UnityEngine;
using UnityEditor;

namespace Shababeek.Springs.Editors
{
    /// <summary>
    /// Custom editor for BaseSpring with validation and scene handles
    /// </summary>
    [CustomEditor(typeof(BaseSpring), true)]
    public class BaseSpringEditor : Editor
    {
        protected float HandleScale = 0.01f;

        private bool _editMode = true;
        private BaseSpring _spring;
        private SerializedProperty _windingsProperty;
        private SerializedProperty _radiusProperty;
        private SerializedProperty _pointsPerWindingProperty;
        private SerializedProperty _taperCurveProperty;

        /// <summary>
        /// Whether scene handles are active
        /// </summary>
        public bool EditMode => _editMode;

        public virtual void OnEnable()
        {
            _spring = (BaseSpring)target;
            _windingsProperty = serializedObject.FindProperty("windings");
            _radiusProperty = serializedObject.FindProperty("radius");
            _pointsPerWindingProperty = serializedObject.FindProperty("pointsPerWinding");
            _taperCurveProperty = serializedObject.FindProperty("taperCurve");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawEditModeToggle();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spring Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            int windings = EditorGUILayout.IntField("Windings", _windingsProperty.intValue);
            if (windings < 1)
            {
                windings = 1;
                EditorGUILayout.HelpBox("Windings must be at least 1.", MessageType.Warning);
            }
            _windingsProperty.intValue = windings;

            int pointsPerWinding = EditorGUILayout.IntField("Points Per Winding", _pointsPerWindingProperty.intValue);
            if (pointsPerWinding < 3)
            {
                pointsPerWinding = 3;
                EditorGUILayout.HelpBox("Points per winding must be at least 3.", MessageType.Warning);
            }
            _pointsPerWindingProperty.intValue = pointsPerWinding;

            float radius = EditorGUILayout.FloatField("Radius", _radiusProperty.floatValue);
            if (radius < 0.01f)
            {
                radius = 0.01f;
                EditorGUILayout.HelpBox("Radius must be at least 0.01.", MessageType.Warning);
            }
            _radiusProperty.floatValue = radius;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_taperCurveProperty, new GUIContent("Taper Curve"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _spring.SetDirty();
                SceneView.RepaintAll();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnSceneGUI()
        {
            if (!_editMode || _spring == null) return;

            Transform springTransform = _spring.transform;
            HandleScale = 0.01f * Mathf.Clamp(springTransform.lossyScale.x, 1f, 5f);

            DrawRadiusHandle(springTransform);
        }

        private void DrawRadiusHandle(Transform springTransform)
        {
            float currentRadius = _spring.Radius;

            Vector3[] handles = new Vector3[4];
            handles[0] = springTransform.TransformPoint(Vector3.right * currentRadius);
            handles[1] = springTransform.TransformPoint(Vector3.forward * currentRadius);
            handles[2] = springTransform.TransformPoint(-Vector3.right * currentRadius);
            handles[3] = springTransform.TransformPoint(-Vector3.forward * currentRadius);

            EditorGUI.BeginChangeCheck();

            Vector3[] newHandles = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Handles.color = Color.yellow;
                newHandles[i] = Handles.FreeMoveHandle(handles[i], HandleScale, Vector3.one, Handles.SphereHandleCap);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spring, "Change Spring Radius");

                float newRadius = currentRadius;
                for (int i = 0; i < 4; i++)
                {
                    if (Vector3.Distance(handles[i], newHandles[i]) > 0.0001f)
                        newRadius = springTransform.InverseTransformPoint(newHandles[i]).magnitude;
                }

                _spring.Radius = Mathf.Max(0.01f, newRadius);
                EditorUtility.SetDirty(_spring);
            }

            // Draw radius circle
            Handles.color = Color.yellow;
            for (float i = 1; i <= 30; i++)
            {
                float a1 = i / 30f * Mathf.PI * 2f;
                float a2 = (i - 1f) / 30f * Mathf.PI * 2f;
                Vector3 p1 = springTransform.TransformPoint(currentRadius * new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)));
                Vector3 p2 = springTransform.TransformPoint(currentRadius * new Vector3(Mathf.Sin(a2), 0f, Mathf.Cos(a2)));
                Handles.DrawLine(p1, p2);
            }

            Handles.DrawLine(newHandles[0], newHandles[2]);
            Handles.DrawLine(newHandles[1], newHandles[3]);

            Vector3 labelPos = springTransform.position + springTransform.right * currentRadius + Vector3.up * 0.2f;
            Handles.Label(labelPos, $"Radius: {currentRadius:F2}");
        }

        private void DrawEditModeToggle()
        {
            EditorGUILayout.Space();
            GUIContent icon = EditorGUIUtility.IconContent(_editMode ? "d_EditCollider" : "EditCollider");
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 32,
                fixedHeight = 24,
                padding = new RectOffset(2, 2, 2, 2)
            };

            Color prevColor = GUI.color;
            if (_editMode) GUI.color = Color.green;
            if (GUILayout.Button(icon, style)) _editMode = !_editMode;
            GUI.color = prevColor;

            EditorGUILayout.Space();
        }
    }
}
