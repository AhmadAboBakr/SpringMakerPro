using UnityEngine;
using UnityEditor;

namespace Shababeek.Springs.Editors
{
    /// <summary>
    /// Custom editor for CurvedSpring with draggable Bézier control points
    /// </summary>
    [CustomEditor(typeof(CurvedSpring))]
    public class CurvedSpringEditor : BaseSpringEditor
    {
        private CurvedSpring _curvedSpring;
        private SerializedProperty _startPointProperty;
        private SerializedProperty _middlePointProperty;
        private SerializedProperty _endPointProperty;

        public override void OnEnable()
        {
            base.OnEnable();
            _curvedSpring = (CurvedSpring)target;
            _startPointProperty = serializedObject.FindProperty("startPoint");
            _middlePointProperty = serializedObject.FindProperty("middlePoint");
            _endPointProperty = serializedObject.FindProperty("endPoint");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Curved Spring Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_startPointProperty, new GUIContent("Start Point"));
            EditorGUILayout.PropertyField(_middlePointProperty, new GUIContent("Middle Point"));
            EditorGUILayout.PropertyField(_endPointProperty, new GUIContent("End Point"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _curvedSpring.SetDirty();
                SceneView.RepaintAll();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnSceneGUI()
        {
            if (_curvedSpring == null || !EditMode) return;
            base.OnSceneGUI();
            DrawControlPointHandles();
        }

        private void DrawControlPointHandles()
        {
            Transform springTransform = _curvedSpring.transform;

            Vector3 worldStart = springTransform.TransformPoint(_curvedSpring.StartPoint);
            Vector3 worldMiddle = springTransform.TransformPoint(_curvedSpring.MiddlePoint);
            Vector3 worldEnd = springTransform.TransformPoint(_curvedSpring.EndPoint);

            EditorGUI.BeginChangeCheck();

            Vector3 newWorldStart = Handles.PositionHandle(worldStart, Quaternion.identity);
            Vector3 newWorldMiddle = Handles.PositionHandle(worldMiddle, Quaternion.identity);
            Vector3 newWorldEnd = Handles.PositionHandle(worldEnd, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_curvedSpring, "Move Curved Spring Control Points");
                _curvedSpring.StartPoint = springTransform.InverseTransformPoint(newWorldStart);
                _curvedSpring.MiddlePoint = springTransform.InverseTransformPoint(newWorldMiddle);
                _curvedSpring.EndPoint = springTransform.InverseTransformPoint(newWorldEnd);
                EditorUtility.SetDirty(_curvedSpring);
            }

            // Labels
            Handles.Label(worldStart + Vector3.up * 0.3f, "Start");
            Handles.Label(worldMiddle + Vector3.up * 0.3f, "Middle");
            Handles.Label(worldEnd + Vector3.up * 0.3f, "End");

            // Draw the underlying Bézier curve
            Handles.color = Color.yellow;
            for (int i = 0; i < 30; i++)
            {
                float a = (float)i / 29f;
                float b = (float)(i + 1) / 29f;
                Handles.DrawLine(
                    springTransform.TransformPoint(_curvedSpring.EvaluateBezier(a)),
                    springTransform.TransformPoint(_curvedSpring.EvaluateBezier(b)));
            }
        }
    }
}
