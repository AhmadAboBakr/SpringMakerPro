using UnityEngine;
using UnityEditor;
using Shababeek.Springs;

namespace Shababeek.Springs.Editors
{
    /// <summary>
    /// Custom editor for SpringMeshGenerator with live preview controls
    /// </summary>
    [CustomEditor(typeof(SpringMeshGenerator))]
    public class SpringMeshGeneratorEditor : Editor
    {
        private SerializedProperty _springProperty;
        private SerializedProperty _sidesProperty;
        private SerializedProperty _tubeRadiusProperty;
        private SerializedProperty _tubeRadiusCurveProperty;
        private SerializedProperty _subdivisionsProperty;
        private SerializedProperty _capsProperty;
        private SerializedProperty _generateTangentsProperty;

        private SpringMeshGenerator _generator;

        private void OnEnable()
        {
            _generator = (SpringMeshGenerator)target;
            _springProperty = serializedObject.FindProperty("spring");
            _sidesProperty = serializedObject.FindProperty("sides");
            _tubeRadiusProperty = serializedObject.FindProperty("tubeRadius");
            _tubeRadiusCurveProperty = serializedObject.FindProperty("tubeRadiusCurve");
            _subdivisionsProperty = serializedObject.FindProperty("subdivisions");
            _capsProperty = serializedObject.FindProperty("caps");
            _generateTangentsProperty = serializedObject.FindProperty("generateTangents");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spring Mesh Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_springProperty, new GUIContent("Spring Reference"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cross-Section", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_sidesProperty, new GUIContent("Sides"));
            EditorGUILayout.PropertyField(_tubeRadiusProperty, new GUIContent("Tube Radius"));
            EditorGUILayout.PropertyField(_tubeRadiusCurveProperty, new GUIContent("Tube Radius Curve"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_subdivisionsProperty, new GUIContent("Subdivisions"));
            EditorGUILayout.PropertyField(_capsProperty, new GUIContent("End Caps"));
            EditorGUILayout.PropertyField(_generateTangentsProperty, new GUIContent("Generate Tangents"));

            if (serializedObject.ApplyModifiedProperties())
                _generator.RegenerateMesh();

            EditorGUILayout.Space();
            DrawMeshStats();

            EditorGUILayout.Space();
            if (GUILayout.Button("Regenerate Mesh"))
                _generator.RegenerateMesh();

            if (GUILayout.Button("Save Mesh Asset"))
                SaveMeshAsset();
        }

        private void DrawMeshStats()
        {
            MeshFilter meshFilter = _generator.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Mesh mesh = meshFilter.sharedMesh;
            EditorGUILayout.LabelField("Mesh Stats", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Vertices", mesh.vertexCount.ToString());
            EditorGUILayout.LabelField("Triangles", (mesh.triangles.Length / 3).ToString());
            EditorGUI.indentLevel--;
        }

        private void SaveMeshAsset()
        {
            MeshFilter meshFilter = _generator.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning("No mesh to save.");
                return;
            }

            string folderPath = "Assets/Shababeek/SpringMaker/Meshes";
            EnsureFolderExists("Assets/Shababeek");
            EnsureFolderExists("Assets/Shababeek/SpringMaker");
            EnsureFolderExists(folderPath);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = $"{folderPath}/SpringTube_{timestamp}.asset";

            Mesh clone = Object.Instantiate(meshFilter.sharedMesh);
            clone.name = $"SpringTube_{timestamp}";
            AssetDatabase.CreateAsset(clone, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = clone;
            EditorGUIUtility.PingObject(clone);
            Debug.Log($"Mesh saved to: {path}");
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path);
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
