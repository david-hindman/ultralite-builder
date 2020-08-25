using UnityEngine;
using UnityEditor;
using System.Linq;

namespace BuilderManager.Builder
{
    public class ConfigEditorWindow : EditorWindow
    {
        private int configIndex;
        private BuildConfiguration config;

        public static void EditConfig(int configIndex)
        {
            var window = GetWindow(typeof(ConfigEditorWindow), true, "Edit Build Configuration") as ConfigEditorWindow;

            window.configIndex = configIndex;
            window.config = BuilderManager.data.Configs[configIndex].DeepClone();
        }

        bool scenesExpanded;

        private Vector2 scrollPos;

        void OnGUI()
        {
            GUI.enabled = !BuilderManager.isBuilding;

            EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.ExpandHeight(true));

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Name", GUILayout.MaxWidth(170f));
            config.name = EditorGUILayout.TextField(config.name);

            EditorGUILayout.LabelField("Product Name", GUILayout.MaxWidth(170f));
            config.productName = EditorGUILayout.TextField(config.productName);

            EditorGUILayout.LabelField("Company Name", GUILayout.MaxWidth(170f));
            config.companyName = EditorGUILayout.TextField(config.companyName);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Platform", GUILayout.MaxWidth(170f));
            config.buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(config.buildTarget);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Development Build", GUILayout.MaxWidth(170f));
            config.developmentBuild = EditorGUILayout.Toggle(config.developmentBuild);

            EditorGUILayout.EndHorizontal();

            GUI.enabled = config.developmentBuild && !BuilderManager.isBuilding;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Allow Debugging", GUILayout.MaxWidth(170f));
            config.allowDebugging = EditorGUILayout.Toggle(config.allowDebugging);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Auto-Attach Profiler", GUILayout.MaxWidth(170f));
            config.autoAttatchProfiler = EditorGUILayout.Toggle(config.autoAttatchProfiler);

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Exclude Streaming Assets", GUILayout.MaxWidth(170f));
            config.excludeStreamingAssets = EditorGUILayout.Toggle(config.excludeStreamingAssets);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Scripting Define Symbols", GUILayout.MaxWidth(170f));
            config.scriptingDefineSymbols = EditorGUILayout.TextField(config.scriptingDefineSymbols);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            EditorGUILayout.LabelField("Scenes", EditorStyles.centeredGreyMiniLabel);

            //EditorGUI.indentLevel = 1;

            for (int i = 0; i < config.scenePaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(config.scenePaths[i]);
                config.scenePaths[i] = AssetDatabase.GetAssetPath(EditorGUILayout.ObjectField(oldScene, typeof(SceneAsset), false));

                if (GUILayout.Button("Remove"))
                {
                    config.scenePaths.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add", GUILayout.Width(75f)))
                config.scenePaths.Add("");

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Output Path:"))
            {
                if (config.BuildTargetExtension != "*")
                    config.outputPath = EditorUtility.SaveFilePanel("Set Output Path", config.outputPath, config.name, config.BuildTargetExtension);
                else
                    config.outputPath = EditorUtility.SaveFolderPanel("Set Output Path", config.outputPath, config.name);
            }

            EditorGUILayout.LabelField(config.outputPath, EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();

            if (config.scenePaths.Count == 0 || !config.scenePaths.Any(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null))
                EditorGUILayout.LabelField("Warning: No scenes have been added to this configuration.", EditorStyles.helpBox);

            if (GUILayout.Button("Save"))
                SaveConfig();

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        private void SaveConfig()
        {
            BuilderManager.data.SetConfig(configIndex, config);
            config = null;

            Close();
        }

        void OnDisable()
        {
            config = null;
        }
    }
}