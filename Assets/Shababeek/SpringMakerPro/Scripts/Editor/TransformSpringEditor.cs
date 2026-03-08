using UnityEngine;
using UnityEditor;

namespace Shababeek.Springs.Editors
{
    /// <summary>
    /// Custom editor for TransformSpring with auto-assignment and scene handles
    /// </summary>
    [CustomEditor(typeof(TransformSpring))]
    public class TransformSpringEditor : BaseSpringEditor
    {
        private TransformSpring _transformSpring;
        private SerializedProperty _t1Property;
        private SerializedProperty _t2Property;
        private SerializedProperty _directionDistanceProperty;

        public override void OnEnable()
        {
            base.OnEnable();
            _transformSpring = (TransformSpring)target;
            _t1Property = serializedObject.FindProperty("t1");
            _t2Property = serializedObject.FindProperty("t2");
            _directionDistanceProperty = serializedObject.FindProperty("directionDistance");

            AutoAssignChildTransforms();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transform Spring Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_t1Property, new GUIContent("Transform 1"));
            EditorGUILayout.PropertyField(_t2Property, new GUIContent("Transform 2"));

            if (GUILayout.Button("Auto-Assign Child Transforms"))
                AutoAssignChildTransforms();

            EditorGUILayout.PropertyField(_directionDistanceProperty, new GUIContent("Direction Distance"));

            if (_t1Property.objectReferenceValue == null || _t2Property.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign both transforms for the spring to work.", MessageType.Warning);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _transformSpring.SetDirty();
                SceneView.RepaintAll();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnSceneGUI()
        {
            if (!EditMode || _transformSpring == null) return;
            base.OnSceneGUI();
            DrawTransformHandles();
        }

        private void DrawTransformHandles()
        {
            Transform tr1 = _t1Property.objectReferenceValue as Transform;
            Transform tr2 = _t2Property.objectReferenceValue as Transform;
            if (tr1 == null || tr2 == null) return;

            EditorGUI.BeginChangeCheck();

            Vector3 newT1Pos = Handles.PositionHandle(tr1.position, tr1.rotation);
            Quaternion newT1Rot = Handles.RotationHandle(tr1.rotation, tr1.position);
            Vector3 newT2Pos = Handles.PositionHandle(tr2.position, tr2.rotation);
            Quaternion newT2Rot = Handles.RotationHandle(tr2.rotation, tr2.position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { tr1, tr2 }, "Move Transform Spring Transforms");
                tr1.position = newT1Pos;
                tr1.rotation = newT1Rot;
                tr2.position = newT2Pos;
                tr2.rotation = newT2Rot;
                EditorUtility.SetDirty(tr1);
                EditorUtility.SetDirty(tr2);
            }

            DrawTransformLabel(tr1, Color.green, "T1");
            DrawTransformLabel(tr2, Color.blue, "T2");
            DrawBezierCurvePreview();
        }

        private void DrawTransformLabel(Transform t, Color color, string label)
        {
            Handles.color = color;
            Vector3 upEnd = t.position + t.up * _transformSpring.DirectionDistance;
            Handles.DrawLine(t.position, upEnd);
            Handles.Label(t.position + Vector3.up * 0.2f, label);
        }

        private void DrawBezierCurvePreview()
        {
            Handles.color = Color.yellow;
            Transform springTransform = _transformSpring.transform;

            for (int i = 0; i < 30; i++)
            {
                float a = (float)i / 29f;
                float b = (float)(i + 1) / 29f;
                Handles.DrawLine(
                    springTransform.TransformPoint(_transformSpring.EvaluateCubicBezier(a)),
                    springTransform.TransformPoint(_transformSpring.EvaluateCubicBezier(b)));
            }
        }

        private void AutoAssignChildTransforms()
        {
            Transform[] children = _transformSpring.GetComponentsInChildren<Transform>();
            int index = 0;

            foreach (Transform child in children)
            {
                if (child == _transformSpring.transform) continue;

                if (index == 0 && _t1Property.objectReferenceValue == null)
                    _t1Property.objectReferenceValue = child;
                else if (index == 1 && _t2Property.objectReferenceValue == null)
                    _t2Property.objectReferenceValue = child;

                index++;
                if (index >= 2) break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
