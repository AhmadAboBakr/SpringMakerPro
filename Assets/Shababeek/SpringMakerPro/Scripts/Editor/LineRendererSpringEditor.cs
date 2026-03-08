using UnityEngine;
using UnityEditor;
using Shababeek.Springs;

namespace Shababeek.Springs.Editors
{
    /// <summary>
    /// Custom editor for LineRendererSpring with material property shortcuts
    /// </summary>
    [CustomEditor(typeof(LineRendererSpring))]
    public class LineRendererSpringEditor : Editor
    {
        private SerializedProperty _springProperty;
        private SerializedProperty _smoothingSegmentsProperty;
        private SerializedProperty _lineWidthProperty;

        private LineRendererSpring _lineRendererSpring;
        private Material _material;

        private void OnEnable()
        {
            _springProperty = serializedObject.FindProperty("spring");
            _smoothingSegmentsProperty = serializedObject.FindProperty("smoothingSegments");
            _lineWidthProperty = serializedObject.FindProperty("lineWidth");
            _lineRendererSpring = (LineRendererSpring)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Line Renderer Spring", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_springProperty, new GUIContent("Spring Reference"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_smoothingSegmentsProperty, new GUIContent("Smoothing Segments"));
            EditorGUILayout.PropertyField(_lineWidthProperty, new GUIContent("Line Width"));

            EditorGUILayout.Space();
            DrawMaterialProperties();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Line"))
                _lineRendererSpring.RefreshLine();
            if (GUILayout.Button("Generate Texture"))
                ApplyGeneratedTexture();
            if (GUILayout.Button("Save Texture"))
                SaveGeneratedTexture();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMaterialProperties()
        {
            LineRenderer lineRenderer = _lineRendererSpring.GetComponent<LineRenderer>();
            if (lineRenderer == null || lineRenderer.sharedMaterial == null)
            {
                EditorGUILayout.HelpBox("Assign a material to the LineRenderer to edit its properties.", MessageType.Info);
                return;
            }

            _material = lineRenderer.sharedMaterial;

            EditorGUILayout.LabelField("Material Properties", EditorStyles.boldLabel);

            DrawColorProperty("_Color", "Base Color");
            DrawSliderProperty("_Alpha", "Alpha", 0f, 1f);

            if (HasAnyProperty("_GlowColor", "_GlowIntensity", "_GlowPower"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Glow Settings", EditorStyles.miniBoldLabel);
                DrawColorProperty("_GlowColor", "Glow Color");
                DrawSliderProperty("_GlowIntensity", "Glow Intensity", 0f, 10f);
                DrawSliderProperty("_GlowPower", "Glow Power", 0.1f, 5f);
            }

            if (HasAnyProperty("_PulseSpeed", "_PulseIntensity", "_ScrollSpeed"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation Settings", EditorStyles.miniBoldLabel);
                DrawSliderProperty("_PulseSpeed", "Pulse Speed", 0f, 5f);
                DrawSliderProperty("_PulseIntensity", "Pulse Intensity", 0f, 2f);
                DrawSliderProperty("_ScrollSpeed", "Scroll Speed", 0f, 10f);
            }

            if (HasAnyProperty("_FresnelPower", "_NoiseScale", "_NoiseIntensity"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advanced Settings", EditorStyles.miniBoldLabel);
                DrawSliderProperty("_FresnelPower", "Fresnel Power", 0.1f, 5f);
                DrawSliderProperty("_NoiseScale", "Noise Scale", 0.1f, 10f);
                DrawSliderProperty("_NoiseIntensity", "Noise Intensity", 0f, 1f);
            }

            EditorUtility.SetDirty(_material);
        }

        private void DrawColorProperty(string propertyName, string label)
        {
            if (!_material.HasProperty(propertyName)) return;
            _material.SetColor(propertyName, EditorGUILayout.ColorField(label, _material.GetColor(propertyName)));
        }

        private void DrawSliderProperty(string propertyName, string label, float min, float max)
        {
            if (!_material.HasProperty(propertyName)) return;
            _material.SetFloat(propertyName, EditorGUILayout.Slider(label, _material.GetFloat(propertyName), min, max));
        }

        private bool HasAnyProperty(params string[] names)
        {
            foreach (string name in names)
                if (_material.HasProperty(name)) return true;
            return false;
        }

        private void ApplyGeneratedTexture()
        {
            LineRenderer lineRenderer = _lineRendererSpring.GetComponent<LineRenderer>();
            if (lineRenderer != null && lineRenderer.sharedMaterial != null)
            {
                Texture2D texture = SpringTextureGenerator.CreateEnergyTexture();
                lineRenderer.sharedMaterial.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(lineRenderer.sharedMaterial);
            }
        }

        private void SaveGeneratedTexture()
        {
            Texture2D texture = SpringTextureGenerator.CreateEnergyTexture();

            string folderPath = "Assets/Shababeek/SpringMaker/Textures";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EnsureFolderExists("Assets/Shababeek");
                EnsureFolderExists("Assets/Shababeek/SpringMaker");
                AssetDatabase.CreateFolder("Assets/Shababeek/SpringMaker", "Textures");
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullPath = $"{folderPath}/SpringLineTexture_{timestamp}.png";

            System.IO.File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(fullPath);
            AssetDatabase.Refresh();

            Texture2D saved = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
            if (saved != null)
            {
                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
            }

            Debug.Log($"Spring texture saved to: {fullPath}");
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
