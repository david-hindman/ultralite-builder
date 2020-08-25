using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BuilderManager.Builder
{
    [Serializable]
    public class Clonable<T>
    {
        public T DeepClone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
    }

    [Serializable]
    public class BuildConfiguration : Clonable<BuildConfiguration>
    {
        [SerializeField]
        public string name;

        [SerializeField]
        public int dataIndex;

        [SerializeField]
        public BuildTarget buildTarget;

        public BuildTargetGroup BuildTargetGroup
        {
            get
            {
                if (buildTarget.ToString().ToLower().Contains("standalone"))
                    return BuildTargetGroup.Standalone;

                else if (buildTarget == BuildTarget.iOS)
                    return BuildTargetGroup.iOS;

                else if (buildTarget == BuildTarget.Android)
                    return BuildTargetGroup.Android;
                else
                    return BuildTargetGroup.Standalone;

                //TODO: fill out the rest of the platforms
            }
        }

        [SerializeField]
        public string
            companyName,
            productName;

        [SerializeField]
        public bool
            excludeStreamingAssets,
            developmentBuild,
            allowDebugging,
            autoAttatchProfiler;

        [SerializeField]
        public string scriptingDefineSymbols;

        [SerializeField]
        public List<string> scenePaths = new List<string>();

        [SerializeField]
        public string outputPath;

        public string BuildTargetExtension
        {
            get
            {
                string ext = "*"; //defaults to no extension (in the case of iOS where a folder is defined rather than an executable)

                switch (buildTarget)
                {
                    case BuildTarget.Android:
                        ext = "apk";
                        break;

                    case BuildTarget.StandaloneWindows:
                        ext = "exe";
                        break;

                    case BuildTarget.StandaloneWindows64:
                        ext = "exe";
                        break;

                        //TODO: Add support for the rest of the platforms
                }

                return ext;
            }
        }

        public BuildConfiguration(int dataIndex, BuildTarget targetPlatform = BuildTarget.StandaloneWindows, string name = "New Build Configuration", bool developmentBuild = false, bool allowDebugging = false, bool excludeStreamingAssets = false)
        {
            this.dataIndex = dataIndex;
            this.buildTarget = targetPlatform;
            this.name = name;
            this.developmentBuild = developmentBuild;
            this.allowDebugging = allowDebugging;
            this.excludeStreamingAssets = excludeStreamingAssets;
            outputPath = Application.dataPath;
        }
    }

    [Serializable]
    public class BuildBatch : Clonable<BuildBatch>
    {
        [SerializeField]
        public string name;

        [SerializeField]
        List<int> configIndices = new List<int>();

        [SerializeField]
        public List<int> ConfigIndices { get { return configIndices; } }

        public void AddConfig(int index)
        {
            configIndices.Add(index);
        }

        public void RemoveConfig(int configIndex)
        {
            configIndices.Remove(configIndex);
        }

        public BuildBatch(List<int> configIndices = null, string name = "New Build Batch")
        {
            this.name = name;

            if (configIndices != null)
                this.configIndices = configIndices;
        }
    }

    [Serializable]
    public class BuilderData : ScriptableObject
    {
        [SerializeField]
        private List<BuildConfiguration> configs = new List<BuildConfiguration>();

        public List<BuildConfiguration> Configs
        {
            get { return configs; }
        }

        public int NumConfigs
        {
            get { return configs.Count; }
        }

        [SerializeField]
        private List<BuildBatch> batches = new List<BuildBatch>();

        public List<BuildBatch> Batches
        {
            get { return batches; }
        }

        public int NumBatches
        {
            get { return batches.Count; }
        }

        public int AddNewConfig()
        {
            configs.Add(new BuildConfiguration(NumConfigs));

            Save();

            return NumConfigs - 1;
        }

        public void RemoveConfig(int index)
        {
            configs.RemoveAt(index);

            foreach (var batch in batches)
                batch.RemoveConfig(index);

            BuilderManager.buildQueue.Remove(index);

            Save();
        }

        public void SetConfig(int configIndex, BuildConfiguration config)
        {
            configs[configIndex] = config;

            Save();
        }

        public int AddNewBatch()
        {
            batches.Add(new BuildBatch());

            Save();

            return NumBatches - 1;
        }

        public void RemoveBatch(int index)
        {
            batches.RemoveAt(index);

            Save();
        }

        public void SetBatch(int index, BuildBatch batch)
        {
            batches[index] = batch;

            Save();
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}