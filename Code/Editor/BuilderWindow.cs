using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BuilderManager.Builder
{
    public class BuilderWindow : EditorWindow
    {
        private class SaveBatchPopup : PopupWindowContent
        {
            private BuildBatch batch = new BuildBatch(BuilderManager.buildQueue);

            public override Vector2 GetWindowSize()
            {
                return new Vector2(175f, 63f);
            }

            public override void OnGUI(Rect rect)
            {
                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField("Save New Batch", EditorStyles.centeredGreyMiniLabel);

                batch.name = EditorGUILayout.TextField(batch.name);

                if (GUILayout.Button("Save"))
                {
                    if (BuilderManager.data.Batches.Any(b => b.name == batch.name))
                    {
                        if (EditorUtility.DisplayDialog("Builder", batch.name + " already exists.  Overwrite it?", "Yes", "No"))
                            OverwriteSaved();
                    }
                    else
                        SaveNew();
                }

                EditorGUILayout.EndVertical();
            }

            private void SaveNew()
            {
                BuilderManager.data.AddNewBatch();
                BuilderManager.data.SetBatch(BuilderManager.data.NumBatches - 1, batch);
                FocusWindowIfItsOpen(typeof(BuilderWindow));
            }

            private void OverwriteSaved()
            {
                int index = BuilderManager.data.Batches.Select(b => b.name).ToList().IndexOf(batch.name);
                BuilderManager.data.SetBatch(index, batch);
                FocusWindowIfItsOpen(typeof(BuilderWindow));
            }
        }

        public static void Open()
        {
            var window = GetWindow(typeof(BuilderWindow)) as BuilderWindow;
            window.titleContent = new GUIContent("Builder", EditorGUIUtility.FindTexture("SettingsIcon"));
            window.Show();
        }

        void OnEnable()
        {
            if (!BuilderManager.isInitialized)
                BuilderManager.Init();
        }

        void OnDisable()
        {
            BuilderManager.Reset();
        }

        private Vector2 scrollPos = Vector2.zero;
        private int selectedMode;

        void OnGUI()
        {
            GUI.enabled = !BuilderManager.isBuilding;

            selectedMode = GUILayout.Toolbar(selectedMode, new string[] { "Queue", "Configurations", "Batches" });

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedMode)
            {
                case 0:
                    DrawQueueEditor();
                    break;

                case 1:
                    DrawConfigsList();
                    break;

                case 2:
                    DrawBatches();
                    break;
            }

            EditorGUILayout.EndScrollView();

            GUI.enabled = true;

            if (BuilderManager.isBuilding)
                BuilderManager.DrawBuildProgressBar();
        }

        private void DrawQueueEditor()
        {
            EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.ExpandHeight(true));

            EditorGUILayout.LabelField("Queued Configurations", EditorStyles.centeredGreyMiniLabel);

            DrawQueuedConfigs();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Config", EditorStyles.miniButtonLeft))
                ShowAddConfigContextMenu();

            GUI.enabled = BuilderManager.data.NumBatches > 0 && !BuilderManager.isBuilding;

            if (GUILayout.Button("Add Batch", EditorStyles.miniButtonRight))
                ShowAddBatchContextMenu();

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (BuilderManager.buildQueue.Count == 0)
                EditorGUILayout.LabelField("Queue is empty.", EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = BuilderManager.buildQueue.Count > 0 && !BuilderManager.isBuilding;

            if (GUILayout.Button("Build Queue", EditorStyles.miniButtonLeft))
            {
                if (EditorUtility.DisplayDialog("Builder", "Build all queued configurations?", "Yes", "No"))
                    BuilderManager.BuildQueue();
            }
            else if (GUILayout.Button("Save as Batch", EditorStyles.miniButtonMid))
                PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), new SaveBatchPopup());

            else if (GUILayout.Button("Remove All", EditorStyles.miniButtonRight))
            {
                if (EditorUtility.DisplayDialog("Builder", "Really remove all queued configurations?", "Yes", "No"))
                    BuilderManager.buildQueue.Clear();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawConfigsList()
        {
            for (int i = 0; i < BuilderManager.data.NumConfigs; i++)
                DrawConfig(i);

            if (GUILayout.Button("Add New", EditorStyles.miniButtonLeft))
                AddNewBuildConfig();
        }

        private void DrawBatches()
        {
            for (int i = 0; i < BuilderManager.data.NumBatches; i++)
                DrawBatch(i);

            if (GUILayout.Button("Add New"))
                AddNewBuildBatch();
        }

        private void DrawConfig(int index)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent(BuilderManager.data.Configs[index].name));

            if (GUILayout.Button("Build", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(80f)))
            {
                if (EditorUtility.DisplayDialog("Builder", "Build " + BuilderManager.data.Configs[index].name + " configuration?", "Yes", "No"))
                    BuilderManager.BuildConfig(index);
            }
            if (GUILayout.Button("Edit", EditorStyles.miniButtonMid, GUILayout.MaxWidth(80f)))
                ConfigEditorWindow.EditConfig(index);
            if (GUILayout.Button("Set Active", EditorStyles.miniButtonMid, GUILayout.MaxWidth(80f)))
            {
                if (EditorUtility.DisplayDialog("Builder", "Set the environment to the " + BuilderManager.data.Configs[index].name + " configuration?", "Yes", "No"))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuilderManager.data.Configs[index].buildTarget);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuilderManager.data.Configs[index].BuildTargetGroup, BuilderManager.data.Configs[index].scriptingDefineSymbols);
                }
            }
            if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.MaxWidth(80f)))
            {
                if (EditorUtility.DisplayDialog("Builder", "Really delete " + BuilderManager.data.Configs[index].name + " configuration?", "Yes", "No"))
                    BuilderManager.data.RemoveConfig(index);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBatch(int index)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent(BuilderManager.data.Batches[index].name));

            if (GUILayout.Button("Build", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(80f)))
            {
                if (EditorUtility.DisplayDialog("Builder", "Build " + BuilderManager.data.Batches[index].name + " batch?", "Yes", "No"))
                    BuilderManager.BuildBatch(index);
            }
            if (GUILayout.Button("Edit", EditorStyles.miniButtonMid, GUILayout.MaxWidth(80f)))
                BatchEditorWindow.EditBatch(index);

            if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.MaxWidth(80f)))
            {
                if (EditorUtility.DisplayDialog("Builder", "Really delete " + BuilderManager.data.Batches[index].name + " batch?", "Yes", "No"))
                    BuilderManager.data.RemoveBatch(index);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddBatchContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (BuildBatch batch in BuilderManager.data.Batches)
                menu.AddItem(new GUIContent(batch.name), false, () => QueueBatch(batch));

            menu.ShowAsContext();
        }

        private void QueueBatch(BuildBatch batch)
        {
            string excludedConfigs = "";

            foreach (var configIndex in batch.ConfigIndices)
            {
                if (!BuilderManager.buildQueue.Contains(configIndex))
                    BuilderManager.buildQueue.Add(configIndex);

                else
                    excludedConfigs += " " + BuilderManager.data.Configs[configIndex].name + ", ";
            }

            if (excludedConfigs != "")
                EditorUtility.DisplayDialog("Builder", "The following configurations were not added because they are already queued: " + excludedConfigs, "Ok");
        }

        private void DrawQueuedConfigs()
        {
            for (int i = 0; i < BuilderManager.buildQueue.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField((i + 1) + ".", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(15f));

                EditorGUILayout.LabelField(BuilderManager.data.Configs[BuilderManager.buildQueue[i]].name);

                if (GUILayout.Button("Build", EditorStyles.miniButtonLeft))
                {
                    if (EditorUtility.DisplayDialog("Builder", "Run solo build for " + BuilderManager.data.Configs[BuilderManager.buildQueue[i]].name + "?", "Yes", "No"))
                        BuilderManager.BuildConfig(BuilderManager.buildQueue[i]);
                }
                else if (GUILayout.Button("Edit", EditorStyles.miniButtonMid))
                    ConfigEditorWindow.EditConfig(BuilderManager.buildQueue[i]);
                else if (GUILayout.Button("Remove", EditorStyles.miniButtonRight))
                    BuilderManager.buildQueue.RemoveAt(i);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void ShowAddConfigContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (var configIndex in BuilderManager.data.Configs.Where(c => !BuilderManager.buildQueue.Contains(c.dataIndex)).Select(c => c.dataIndex))
                menu.AddItem(new GUIContent(BuilderManager.data.Configs[configIndex].name), false, () => BuilderManager.buildQueue.Add(configIndex));

            menu.AddItem(new GUIContent("*Create New*"), false, () =>
            {
                AddNewBuildConfig();
                BuilderManager.buildQueue.Add(BuilderManager.data.Configs[BuilderManager.data.Configs.Count - 1].dataIndex);
            });

            menu.ShowAsContext();
        }

        public void AddNewBuildConfig()
        {
            ConfigEditorWindow.EditConfig(BuilderManager.data.AddNewConfig());
        }

        private void AddNewBuildBatch()
        {
            BatchEditorWindow.EditBatch(BuilderManager.data.AddNewBatch());
        }
    }
}