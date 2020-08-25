using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BuilderManager.Builder
{
    public class BatchEditorWindow : EditorWindow
    {
        private int batchIndex;
        private BuildBatch batch;

        public static void EditBatch(int batchIndex)
        {
            var window = GetWindow(typeof(BatchEditorWindow), true, "Edit Build Batch") as BatchEditorWindow;
            window.name = "Edit Batch";
            window.batchIndex = batchIndex;
            window.batch = BuilderManager.data.Batches[batchIndex];
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            batch.name = EditorGUILayout.TextField(batch.name);

            foreach (var configIndex in batch.ConfigIndices)
            {
                bool wasRemoved = DrawConfig(configIndex);

                if (wasRemoved)
                    break;
            }

            if (GUILayout.Button("Add Config"))
                ShowAddConfigContextMenu();

            if (GUILayout.Button("Save"))
                SaveBatch();

            EditorGUILayout.EndVertical();
        }

        private bool DrawConfig(int configIndex)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(BuilderManager.data.Configs[configIndex].name);

            if (GUILayout.Button("Remove"))
            {
                batch.RemoveConfig(configIndex);
                return true;
            }

            EditorGUILayout.EndHorizontal();

            return false;
        }

        private void ShowAddConfigContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (var config in BuilderManager.data.Configs.Where(c => !batch.ConfigIndices.Contains(c.dataIndex)))
                menu.AddItem(new GUIContent(config.name), false, () => batch.AddConfig(config.dataIndex));

            menu.AddItem(new GUIContent("*Create New*"), false, () =>
            {
                ((BuilderWindow)GetWindow(typeof(BuilderWindow))).AddNewBuildConfig();
                batch.ConfigIndices.Add(BuilderManager.data.Configs[BuilderManager.data.NumConfigs - 1].dataIndex);
                Repaint();
            });

            menu.ShowAsContext();
        }

        private void SaveBatch()
        {
            BuilderManager.data.SetBatch(batchIndex, batch);
            batch = null;

            Close();
        }

        void OnDisable()
        {
            batch = null;
        }
    }
}