using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Linq;

namespace BuilderManager.Builder
{
    public static class BuilderManager
    {
        public static readonly string
            ASSET_NAME = "UltraliteBuilderData.asset",
            ASSET_EDITOR_PATH = "/Ultralite/Editor/Builder/";

        public static BuilderData data;

        public static bool
            isInitialized,
            isBuilding;

        [MenuItem("Window/Ultralite/Builder", false, 2009)]
        public static void OpenEditorWindow()
        {
            Init();

            BuilderWindow.Open();
        }

        public static void Init()
        {
            if (!Directory.Exists(Application.dataPath + ASSET_EDITOR_PATH))
                Directory.CreateDirectory(Application.dataPath + ASSET_EDITOR_PATH);

            if (!File.Exists(Application.dataPath + ASSET_EDITOR_PATH + ASSET_NAME))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BuilderData>(), "Assets" + ASSET_EDITOR_PATH + ASSET_NAME);

            data = AssetDatabase.LoadAssetAtPath("Assets" + ASSET_EDITOR_PATH + ASSET_NAME, typeof(BuilderData)) as BuilderData;

            buildQueue = new List<int>();

            isInitialized = true;
            isBuilding = false;
        }

        public static List<int> buildQueue;
        private static IEnumerator<BuildConfiguration> currentlyBuildingConfigs;
        private static int numBuilds;
        private static int completedBuilds = 0;

        public static void BuildQueue()
        {
            KickBuild(data.Configs.Where(config => buildQueue.Contains(config.dataIndex)).ToList());
        }

        public static void BuildBatch(BuildBatch batch)
        {
            isBuilding = true;

            var configs = data.Configs.Where(config => batch.ConfigIndices.Contains(config.dataIndex)).ToList();

            KickBuild(configs);
        }

        public static void BuildBatch(int index)
        {
            isBuilding = true;

            BuildBatch(data.Batches[index]);
        }

        public static void BuildConfig(BuildConfiguration config)
        {
            var list = new List<BuildConfiguration>();
            list.Add(config);

            isBuilding = true;

            KickBuild(list);
        }

        public static void BuildConfig(int index)
        {
            isBuilding = true;

            BuildConfig(data.Configs[index]);
        }

        private static void KickBuild(List<BuildConfiguration> configs)
        {
            currentlyBuildingConfigs = configs.GetEnumerator();

            currentlyBuildingConfigs.MoveNext();

            isBuilding = true;

            completedBuilds = 0;
            numBuilds = configs.Count;

            BuildNext();
        }

        private static void BuildNext()
        {
            BuildConfiguration config = currentlyBuildingConfigs.Current;

            var buildOptions = new BuildPlayerOptions();

            buildOptions.scenes = config.scenePaths.ToArray();
            buildOptions.target = config.buildTarget;
            buildOptions.locationPathName = config.outputPath;
            buildOptions.options = config.developmentBuild ? buildOptions.options | BuildOptions.Development : buildOptions.options;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(config.BuildTargetGroup, config.scriptingDefineSymbols);

            if (!string.IsNullOrEmpty(config.productName))
                PlayerSettings.productName = config.productName;
            if (!string.IsNullOrEmpty(config.companyName))
                PlayerSettings.companyName = config.companyName;

            if (buildOptions.options == BuildOptions.Development)
            {
                buildOptions.options = config.allowDebugging ? buildOptions.options | BuildOptions.AllowDebugging : buildOptions.options;
                buildOptions.options = config.autoAttatchProfiler ? buildOptions.options | BuildOptions.ConnectWithProfiler : buildOptions.options;
            }

            if (config.excludeStreamingAssets)
                Directory.Move(Application.streamingAssetsPath, Application.dataPath + "/.StreamingAssets");

            BuildPipeline.BuildPlayer(buildOptions);
        }

        private static double nextBuildTime;

        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (!isBuilding)
                return;

            if (currentlyBuildingConfigs.Current.excludeStreamingAssets)
                Directory.Move(Application.dataPath + "/.StreamingAssets", Application.streamingAssetsPath);

            if (currentlyBuildingConfigs.MoveNext())
            {
                nextBuildTime = EditorApplication.timeSinceStartup + 1f;
                EditorApplication.update += WaitToBuildNext;
            }
            else
            {
                currentlyBuildingConfigs.Dispose();
                isBuilding = false;
            }
        }

        private static void WaitToBuildNext()
        {
            if (EditorApplication.timeSinceStartup >= nextBuildTime)
            {
                EditorApplication.update -= WaitToBuildNext;
                completedBuilds++;
                BuildNext();
            }
        }

        public static void Reset()
        {
            isInitialized = false;
            isBuilding = false;
        }

        public static void DrawBuildProgressBar()
        {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(18f)), (float)completedBuilds / (float)numBuilds, "Building " + currentlyBuildingConfigs.Current.name + "... (" + (completedBuilds + 1) + "/" + numBuilds + ")");
            EditorGUILayout.Space();
        }
    }
}
