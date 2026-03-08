using UnityEngine;
using UnityEditor;

namespace Shababeek.Springs.Editors
{
    /// <summary>
    /// Custom editor for SimpleSpring with a height scene handle
    /// </summary>
    [CustomEditor(typeof(SimpleSpring))]
    public class SimpleSpringEditor : BaseSpringEditor
    {
        private SimpleSpring _simpleSpring;
        private SerializedProperty _heightProperty;

        public override void OnEnable()
        {
            base.OnEnable();
            _simpleSpring = (SimpleSpring)target;
            _heightProperty = serializedObject.FindProperty("height");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simple Spring Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            float height = EditorGUILayout.FloatField("Height", _heightProperty.floatValue);
            if (height < 0.01f)
            {
                height = 0.01f;
                EditorGUILayout.HelpBox("Height must be at least 0.01.", MessageType.Warning);
            }
            _heightProperty.floatValue = height;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _simpleSpring.SetDirty();
                SceneView.RepaintAll();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnSceneGUI()
        {
            if (_simpleSpring == null || !EditMode) return;
            base.OnSceneGUI();
            DrawHeightHandle();
        }

        private void DrawHeightHandle()
        {
            Transform springTransform = _simpleSpring.transform;
            float currentHeight = _simpleSpring.Height;

            Vector3 handlePos = springTransform.TransformPoint(Vector3.up * currentHeight);
            Handles.color = Color.green;

            EditorGUI.BeginChangeCheck();
            Vector3 newHandlePos = Handles.FreeMoveHandle(handlePos, HandleScale, Vector3.one, Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_simpleSpring, "Change Spring Height");
                float newHeight = Mathf.Max(0.01f, springTransform.InverseTransformPoint(newHandlePos).y);
                _simpleSpring.Height = newHeight;
                EditorUtility.SetDirty(_simpleSpring);
            }

            Handles.DrawLine(springTransform.position, handlePos);
            Handles.Label(handlePos + Vector3.right * 0.2f, $"Height: {currentHeight:F2}");
        }
    }
}
